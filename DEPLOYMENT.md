# Prelazak produkcije s `main` na novu verziju

Upute za prvi deploy ove grane na postojeću produkcijsku bazu. Postojeći korisnici i podaci ostaju. Baza se mijenja migracijama, a ne ručno.

**Migracije se primjenjuju same.** Deploy workflow za API (`deploy-server.yml`), prije nego što raspakira novi API, na serveru pokreće `database/deploy/migrate.sh`. Ona:
1. po potrebi označi `Initial` kao primijenjenu (`baseline.sql`, samo na bazi sa starom shemom),
2. provjeri ima li migracija koje još nisu primijenjene i, ako nema, ne radi ništa,
3. inače napravi backup i primijeni sve migracije **u jednoj transakciji**.

Ako migracija padne, baza ostaje kakva je bila, workflow staje, a stari API radi dalje. Detalji su u poglavlju 5.

Kratko:
1. Proba na kopiji produkcijske baze (ručno, poglavlje 4).
2. Merge u `main`: workflow migrira bazu i deploya API, a drugi workflow deploya klijent.
3. Provjera u pregledniku.

API i klijent idu **zajedno**. Rute API-ja su promijenjene (npr. `/auth/login` → `/api/auth/login`), pa stari klijent ne radi s novim API-jem i obrnuto. Oba workflowa pokreće isti merge.

---

## 1. Što se mijenja u bazi

Migracije su u `server/Loom.Infrastructure/Migrations`. Ovim redom:

| Migracija | Što radi s postojećim podacima |
|---|---|
| `Initial` | Ništa. Opisuje shemu koju produkcija već ima (stari `schema.sql`). `baseline.sql` je samo označi kao primijenjenu. |
| `AddForeignKeyIndexes` | Dodaje indekse. Podaci se ne mijenjaju. |
| `AddConcurrencyTokens` | Ništa fizički. Koristi postojeću Postgres kolonu `xmin`. |
| `AddAccessLinks` | Nova tablica `exchange.access_link`. Stari linkovi (`/access/<guid>`) za razmjene čiji student još nije preuzeo razmjenu prenose se, pa i dalje rade. |
| `DocumentVersions` | Nova tablica `exchange.document_version`. Sve iz `exchange.snapshot` se kopira u nju (odobrenja dobivaju brojeve 1, 2, …, a "PreImport" postaje backup). Ocjene iz stare `exchange.recognition_entry` prelaze u rezultate. Statusi `Submitted` i `Rejected` postaju `Draft` (UI ih nikad nije koristio). Tek nakon toga se `exchange.snapshot` i stara `exchange.recognition_entry` brišu. |
| `AccentInsensitiveSearch` | Ekstenzije `unaccent` i `pg_trgm`, funkcija `public.f_unaccent` i indeksi za pretragu korisnika. Podaci se ne mijenjaju. |
| `SplitRecognitionResults` | Nova `exchange.recognition_entry`: jedan rezultat (status, ocjene) po kolegiju. `exchange.mapping_scheme_entry` ostaje samo raspored (mjesto i ECTS). "Priznavanje započeto" seli se s `learning_agreement` na `recognition` (`started_at`, `started_by`). |

**Gdje se podaci mijenjaju ili gube:**
- Retci u `mapping_scheme_entry` čiji je partnerski kolegij obrisan (`partner_course_id IS NULL`) brišu se, jer nemaju kolegij kojem bi pripadali.
- Ako je isti kolegij bio na više mjesta s **različitim** ocjenama, ostaje ocjena iz zadnje promijenjenog retka koji ima ocjenu.
- Dva retka istog kolegija na istom mjestu spajaju se u jedan, a ECTS se zbraja.
- Dokumenti u statusu `Submitted` ili `Rejected` postaju `Draft`.

Sve ostalo se samo prenosi.

---

## 2. Preduvjeti (provjeriti jednom)

- **PostgreSQL 13 ili noviji.** Provjera: `SELECT version();`
- **Ekstenzije `unaccent` i `pg_trgm`:**
  - Obje su "trusted", pa ih vlasnik baze može napraviti sam.
  - Ako aplikacijski korisnik nema pravo `CREATE` na bazi, admin jednom pokrene:
    ```sql
    CREATE EXTENSION IF NOT EXISTS unaccent SCHEMA public;
    CREATE EXTENSION IF NOT EXISTS pg_trgm SCHEMA public;
    ```
- **Na serveru (za automatsku migraciju):**
  - `psql`, `pg_dump` i `pg_restore` instalirani (paket `postgresql-client`, ista ili novija glavna verzija od servera baze).
  - SSH korisnik iz workflowa (`USERNAME`) može pisati u `~/loom-backups` ili u mapu zadanu varijablom `BACKUP_DIR`.
  - Korisnik baze (`DB_USERNAME`) je vlasnik shema (`public`, `home`, `partner`, `exchange`), jer migracije mijenjaju tablice.
- **Za probu (poglavlje 4) na računalu s kojeg se radi:** `psql`, `pg_dump`, `pg_restore` i .NET 10 SDK ili Docker (za generiranje skripte).
- **GitHub secrets i variables:**
  - Ostaju isti kao na `main`: `USERNAME`, `PASSWORD`, `DB_USERNAME`, `DB_PASSWORD`, `CLIENT_ID`, `CLIENT_SECRET`, `VITE_GOOGLE_CLIENT_ID`, …
  - Workflow za migraciju koristi postojeće `DB_USERNAME`, `DB_PASSWORD` i varijable `DB_HOST`, `DB_PORT`, `DB_NAME`, gledano sa servera (isto kao API).
  - Novo i opcionalno:
    - `BACKUP_DIR`: mapa za backup prije migracije, na serveru.
    - `VITE_SENTRY_DSN` (klijent) i `Sentry__Dsn` (API): bez njih je praćenje grešaka isključeno.
  - **Odobrenje deploya (preporučeno):** job koristi GitHub environment `production`. U *Settings → Environments → production* dodati *Required reviewers*. Tada svaki deploy API-ja, pa i migracija, čeka da ga netko odobri.
- **Google OAuth:** callback je i dalje `<API URL>/signin-oidc`. U Google Consoleu ne treba ništa mijenjati.
- **nginx:** ako proxy prosljeđuje cijeli prefiks API-ja (npr. `/loom-api/` → `localhost:6010`), ne treba ništa mijenjati. Ako postoje pravila za pojedine putanje (`/auth/...`, `/exchanges/...`), treba ih prilagoditi. Sve rute su sada pod `/api/...`.
- **PgBouncer:** ako je između API-ja i baze PgBouncer u transaction modu, postaviti `Database__DisableJit=false` i `ALTER ROLE <app user> SET jit = off;`. Bez PgBouncera ne treba ništa.

---

## 3. Generiranje skripte za migraciju

Skripta je idempotentna: preskače migracije koje su već primijenjene, pa se smije pokrenuti i više puta. Generira se iz ove grane, s `--no-transactions`, da bi se cijela izvršila kao **jedna transakcija** (`psql -1`, korak 5). Ako bilo što padne, baza ostaje točno kakva je bila.

```sh
dotnet tool restore
dotnet ef migrations script --idempotent --no-transactions \
  --project server/Loom.Infrastructure --startup-project server/Loom.Infrastructure \
  -o migrations.sql
```

Bez .NET SDK-a, isto preko Dockera (iz korijena repozitorija):

```sh
docker run --rm -v "$PWD":/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 sh -c \
  'dotnet tool restore && dotnet ef migrations script --idempotent --no-transactions --project server/Loom.Infrastructure --startup-project server/Loom.Infrastructure -o migrations.sql'
```

Deploy workflow generira istu skriptu i šalje je na server, a prilaže je i kao artefakt `migrations-sql` svakog runa. Lokalno je treba generirati samo za probu (poglavlje 4) ili za ručnu migraciju.

---

## 4. Proba na kopiji (prije pravog deploya)

Cijeli postupak prvo napraviti na kopiji produkcijske baze. Produkcija se pritom ne dira.

```sh
export PGHOST=<host> PGUSER=<vlasnik baze>       # lozinka u ~/.pgpass
# 1. Kopija produkcije
PGDATABASE=<prod baza> ./database/backup/backup.sh ./backup-proba
./database/backup/restore.sh ./backup-proba/daily/<datoteka>.dump loom_proba

# 2. Brojke prije migracije (spremiti izlaz)
psql -d loom_proba -f - <<'SQL'
SELECT 'exchanges', count(*) FROM exchange.exchange
UNION ALL SELECT 'la entries', count(*) FROM exchange.learning_agreement_entry
UNION ALL SELECT 'snapshots', count(*) FROM exchange.snapshot
UNION ALL SELECT 'scheme rows', count(*) FROM exchange.mapping_scheme_entry
UNION ALL SELECT 'scheme rows bez kolegija', count(*) FROM exchange.mapping_scheme_entry WHERE partner_course_id IS NULL
UNION ALL SELECT 'kolegiji s rezultatom', count(DISTINCT (exchange_id, partner_course_id)) FROM exchange.mapping_scheme_entry WHERE partner_course_id IS NOT NULL
UNION ALL SELECT 'stari recognition_entry', count(*) FROM exchange.recognition_entry;
SQL

# 3. Migracija
psql -d loom_proba -v ON_ERROR_STOP=1 -f database/baseline.sql
psql -d loom_proba -v ON_ERROR_STOP=1 -1 -f migrations.sql
```

**Provjera nakon migracije:**

```sql
SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY 1;   -- 7 redaka, zadnji ..._SplitRecognitionResults
SELECT 'exchanges', count(*) FROM exchange.exchange                                  -- isto kao prije
UNION ALL SELECT 'versions', count(*) FROM exchange.document_version                 -- = snapshots
UNION ALL SELECT 'results', count(*) FROM exchange.recognition_entry                 -- >= "kolegiji s rezultatom"
UNION ALL SELECT 'placements', count(*) FROM exchange.mapping_scheme_entry           -- <= scheme rows - bez kolegija
UNION ALL SELECT 'started', count(*) FROM exchange.recognition WHERE started_at IS NOT NULL;
```

- **`results`** može biti veći od "kolegiji s rezultatom". To su razmjene čije su ocjene bile samo u staroj `recognition_entry`.
- **`placements`** je manji samo ako je bilo duplih redaka (isti kolegij na istom mjestu).

**Klikanje po aplikaciji na kopiji (preporučeno):** pokrenuti API lokalno na `loom_proba` i otvoriti nekoliko stvarnih razmjena (LA, povijest verzija, priznavanje, mapping scheme, službeni xlsx):

```sh
ConnectionStrings__DefaultConnection="Host=<host>;Database=loom_proba;Username=<user>;Password=<pass>" \
  dotnet run --project server/Loom.Api
```

**Opcionalno, provjera da produkcijska shema odgovara `Initial`:**

```sh
createdb loom_initial
dotnet ef database update 20261008214540_Initial --project server/Loom.Infrastructure \
  --startup-project server/Loom.Infrastructure --connection "Host=<host>;Database=loom_initial;Username=<user>;Password=<pass>"
psql -d <prod baza>   -tA -f database/verify-schema.sql > prod.txt
psql -d loom_initial -tA -f database/verify-schema.sql > initial.txt
diff prod.txt initial.txt      # očekivano prazno
```

Ako proba prođe, kopije se mogu obrisati (`dropdb loom_proba loom_initial`).

---

## 5. Deploy (pravi)

Planirati kratak prekid rada (oko 10 minuta), po mogućnosti kad nitko ne radi. Tijekom migracije stari API još radi, ali dio zahtjeva može vraćati greške dok se novi API ne restarta.

1. **Najaviti prekid** korisnicima.
2. **Merge grane u `main`.** Pokreću se oba workflowa:
   - **`Loom WebApi`** (`deploy-server.yml`): build, publish, tarball na server, pa korak *Migrate database and unpack*. Taj korak:
     - raspakira samo `db/`,
     - pokrene `migrate.sh` (baseline, backup, migracije u jednoj transakciji),
     - tek onda raspakira novi API i postavi `restart_required.txt`.
   - **`Loom Client`** (`deploy-client.yml`): build i statičke datoteke.

   Ako je u environmentu `production` uključeno odobrenje, `Loom WebApi` čeka klik na *Review deployments → Approve*.
3. **Pratiti log koraka *Migrate database and unpack*.** Treba pisati:
   - popis migracija koje se primjenjuju (*pending migrations*),
   - `backup ok: <datoteka>` (zapisati ime datoteke),
   - `migrate: done`.
4. **Ako migracija padne:**
   - Korak je crven, baza je nepromijenjena i stari API radi dalje.
   - Klijent je možda već deployan, a novi klijent ne radi sa starim API-jem. Ponovno pokrenuti `Loom Client` na prethodnom commitu (ili revertati merge) i javiti grešku iz loga.
   - Povratak iz backupa nije potreban.
5. **Provjera:** isti upiti kao u probi (poglavlje 4). Brojke trebaju odgovarati probi.
6. **Provjera u pregledniku:**
   - Prijava preko Googlea radi.
   - Otvara se nekoliko razmjena: LA, povijest verzija, priznavanje (tablica 1 i 2), mapping scheme.
   - Generira se službeni xlsx.
   - Koordinator vidi listu studenata, a admin listu korisnika.
   - U `logs/loom-errors-<datum>.log` nema novih grešaka.
7. **Najaviti kraj.** Korisnici će se vjerojatno morati ponovno prijaviti. Ništa se ne gubi, samo sesija.

**Poslije deploya:**
- Svaki sljedeći merge u `main` radi isto: ako nema novih migracija, `migrate.sh` samo javi *database is up to date* i ne radi backup.
- Postaviti noćni backup (`database/backup/loom-backup.service` i `.timer`, primjer za systemd).

**Ručno, bez workflowa** (ako server nema `psql` ili workflow ne smije dirati bazu), prije merga:
```sh
PGDATABASE=<prod baza> ./database/backup/backup.sh /var/backups/loom
psql -d <prod baza> -v ON_ERROR_STOP=1 -f database/baseline.sql
psql -d <prod baza> -v ON_ERROR_STOP=1 -1 -f migrations.sql     # poglavlje 3
```
Workflow će nakon toga javiti *database is up to date*.

---

## 6. Povratak na staro (ako nešto pođe po zlu)

Najsigurnije je vratiti backup koji je `migrate.sh` napravio prije migracije (ime datoteke je u logu workflowa), a ne pokretati Down migracije.

1. Zaustaviti novi API.
2. Vratiti backup u **novu** bazu:
   ```sh
   ./database/backup/restore.sh ~/loom-backups/daily/<datoteka>.dump loom_restore
   ```
3. Prebaciti aplikaciju na nju: zamijeniti imena baza (`ALTER DATABASE ... RENAME TO ...`) ili promijeniti connection string.
4. Ponovno deployati stari `main`: revert merge commita u `main`, ili ručno pokrenuti oba workflowa na prethodnom commitu.

Sve što su korisnici unijeli između deploya i povratka se gubi, pa povratak treba odlučiti brzo.
