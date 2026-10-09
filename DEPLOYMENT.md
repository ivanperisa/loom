# Prelazak produkcije s `main` na novu verziju

Upute za prvi deploy ove grane na postojeću produkcijsku bazu. Postojeći korisnici i podaci ostaju. Baza se mijenja migracijama, a ne ručno.

Kratko:
1. Proba na kopiji produkcijske baze.
2. Na dan deploya: zaustaviti API, backup, `baseline.sql`, `migrations.sql`, provjera, merge u `main` (deploy API-ja i klijenta), provjera u pregledniku.

API i klijent idu **zajedno**. Rute API-ja su promijenjene (npr. `/auth/login` → `/api/auth/login`), pa stari klijent ne radi s novim API-jem i obrnuto. Stari API ne radi ni na novoj shemi. Zato se migracija i deploy rade jedno za drugim, u istom kratkom prozoru.

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
- **Alati na računalu s kojeg se radi:** `psql`, `pg_dump`, `pg_restore` (iste ili novije verzije od servera) i .NET 10 SDK ili Docker (za generiranje skripte).
- **GitHub secrets i variables:**
  - Ostaju isti kao na `main`: `USERNAME`, `PASSWORD`, `DB_USERNAME`, `DB_PASSWORD`, `CLIENT_ID`, `CLIENT_SECRET`, `VITE_GOOGLE_CLIENT_ID`, …
  - Novo i opcionalno: `VITE_SENTRY_DSN` (klijent) i `Sentry__Dsn` (API). Bez njih je praćenje grešaka isključeno.
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

Deploy workflow (`deploy-server.yml`) prilaže i artefakt `migrations-sql` svakog runa. On je generiran bez `--no-transactions`, pa svaka migracija ima svoju transakciju (ako jedna padne, prethodne ostaju). Za prvi deploy skriptu treba imati **prije** merga, pa je generirati lokalno.

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

Planirati kratak prekid rada (oko 10–15 minuta), po mogućnosti kad nitko ne radi.

1. **Najaviti prekid** korisnicima.
2. **Zaustaviti stari API** (onako kako se inače zaustavlja ili restarta na serveru). Tako nitko ne piše u bazu tijekom migracije.
3. **Backup:**
   ```sh
   PGDATABASE=<prod baza> ./database/backup/backup.sh /var/backups/loom
   ```
   Skripta sama provjeri da se dump da pročitati. Zapisati ime datoteke.
4. **Baseline** (jednom, smije se ponoviti):
   ```sh
   psql -d <prod baza> -v ON_ERROR_STOP=1 -f database/baseline.sql
   ```
5. **Migracije, sve u jednoj transakciji:**
   ```sh
   psql -d <prod baza> -v ON_ERROR_STOP=1 -1 -f migrations.sql
   ```
   - `-1` znači jednu transakciju, a `ON_ERROR_STOP` zaustavlja izvođenje na prvoj grešci. Tada se poništava **sve** i baza ostaje kakva je bila prije koraka 5.
   - Nakon greške: pokrenuti stari API i javiti grešku. Povratak iz backupa (poglavlje 6) tada nije potreban.
6. **Provjera:** isti upiti kao u probi (poglavlje 4). Brojke trebaju odgovarati probi.
7. **Merge grane u `main`.** Na push u `main` se pokreću oba workflowa:
   - `Loom WebApi` (`deploy-server.yml`): build, publish, tarball na server i `restart_required.txt`.
   - `Loom Client` (`deploy-client.yml`): build i statičke datoteke.

   Pričekati da oba završe zeleno i da se API restarta. Ako se automatski ne pokrenu, pokrenuti ih ručno (Actions → workflow → *Run workflow*).
8. **Provjera u pregledniku:**
   - Prijava preko Googlea radi.
   - Otvara se nekoliko razmjena: LA, povijest verzija, priznavanje (tablica 1 i 2), mapping scheme.
   - Generira se službeni xlsx.
   - Koordinator vidi listu studenata, a admin listu korisnika.
   - U `logs/loom-errors-<datum>.log` nema novih grešaka.
9. **Najaviti kraj.** Korisnici će se vjerojatno morati ponovno prijaviti. Ništa se ne gubi, samo sesija.

Poslije deploya:
- Postaviti noćni backup (`database/backup/loom-backup.service` i `.timer`, primjer za systemd).
- Svaki sljedeći deploy koji donosi nove migracije ide istim redom: backup → `migrations.sql` (iz artefakta tog runa ili lokalno) → deploy. `baseline.sql` više ne treba.

---

## 6. Povratak na staro (ako nešto pođe po zlu)

Najsigurnije je vratiti backup iz koraka 3, a ne pokretati Down migracije.

1. Zaustaviti novi API.
2. Vratiti backup u **novu** bazu:
   ```sh
   ./database/backup/restore.sh /var/backups/loom/daily/<datoteka>.dump loom_restore
   ```
3. Prebaciti aplikaciju na nju: zamijeniti imena baza (`ALTER DATABASE ... RENAME TO ...`) ili promijeniti connection string.
4. Ponovno deployati stari `main`: revert merge commita u `main`, ili ručno pokrenuti oba workflowa na prethodnom commitu.

Sve što su korisnici unijeli između deploya i povratka se gubi, pa povratak treba odlučiti brzo.
