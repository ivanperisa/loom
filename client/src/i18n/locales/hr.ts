export default {
  common: {
    appName: 'ExchangeMapper',
    signIn: 'Prijava putem Googlea',
    signOut: 'Odjava',
    loading: 'Učitavanje...',
    error: 'Došlo je do greške.',
    na: 'N/A',
    language: 'Jezik',
    user: 'Korisnik'
  },
  auth: {
    signInWithGoogle: 'Prijava putem Googlea'
  },
  landing: {
    tagline: 'Pojednostavite preslikavanje kolegija za Erasmus',
    description: 'Preslikajte kolegije s razmjene, pratite ECTS bodove i dobijte odobrenje koordinatora - sve na jednom mjestu.',
    visualTitle: 'Preslikavanje kolegija jednostavno'
  },
  home: {
    welcome: 'Dobrodošli, {name}',
    noExchange: 'Nemate aktivnu razmjenu. Krenite ispod.',
    startExchange: 'Postavi razmjenu',
    institution: 'Matični fakultet',
    studyProgram: 'Studijski program',
    studyProfile: 'Studijski profil'
  },
  onboarding: {
    title: 'Postavljanje računa',
    steps: {
      role: 'Uloga',
      institutions: 'Institucije',
      confirm: 'Potvrda'
    },
    role: {
      title: 'Odaberite svoju ulogu',
      student: 'Student',
      coordinator: 'Koordinator',
      coordinatorNote: 'Vaša uloga bit će potvrđena.'
    },
    institutions: {
      title: 'Odaberite vaše institucije',
      subtitle: 'Dodajte sve fakultete na kojima studirate.',
      addToList: 'Dodaj u listu',
      added: 'Dodane institucije',
      empty: 'Niste dodali nijednu instituciju.',
      minOne: 'Dodajte barem jednu instituciju za nastavak.',
      duplicate: 'Ova kombinacija institucije i profila već je dodana.',
      existingOption: 'Postojeća institucija',
      newOption: 'Nova institucija',
      searchPlaceholder: 'Pretraži institucije...',
      selectProgram: 'Odaberi program',
      selectProfile: 'Odaberi profil',
      newProfileOption: '+ Novi profil',
      newProfilePlaceholder: 'Naziv novog profila',
      form: {
        name: 'Naziv institucije',
        country: 'Država',
        city: 'Grad',
        erasmusCode: 'Erasmus kod',
        iscedCode: 'ISCED kod',
        programName: 'Naziv programa',
        profileName: 'Naziv profila'
      }
    },
    confirm: {
      title: 'Pregled podataka',
      submit: 'Završi postavljanje',
      role: 'Uloga',
      institution: 'Institucija',
      program: 'Program',
      profile: 'Profil'
    },
    next: 'Sljedeći',
    back: 'Natrag',
    noResults: 'Nije pronađeno.',
    errors: {
      selectRole: 'Odaberite ulogu prije nastavka.',
      institutionRequired: 'Odaberite instituciju ili unesite podatke za novu.',
      programProfileRequired: 'Odaberite studijski program i profil ili unesite novi profil.'
    }
  },
  errors: {
    required: 'Ovo polje je obavezno.',
    notFound: 'Nije pronađeno.',
    unexpected: 'Došlo je do neočekivane greške.',
    unauthorized: 'Nemate pristup ovoj stranici.'
  },
  nav: {
    home: 'Početna',
    settings: 'Postavke',
    exchange: 'Razmjena',
    history: 'Povijest'
  },
  settings: {
    title: 'Postavke',
    profile: {
      title: 'Profil',
      name: 'Ime',
      email: 'E-mail adresa',
      role: 'Uloga'
    },
    institutions: {
      title: 'Moje institucije',
      empty: 'Nemate dodanih institucija.',
      program: 'Program',
      profile: 'Profil',
      edit: 'Uredi',
      editDisabledTooltip: 'Nije moguće urediti instituciju s aktivnim razmjenama.',
      save: 'Spremi',
      cancel: 'Odustani',
      remove: 'Ukloni',
      addButton: 'Dodaj instituciju',
      addTitle: 'Dodaj instituciju',
      removeConfirm: 'Jeste li sigurni da želite ukloniti ovu instituciju?',
      removeError: 'Nije moguće ukloniti instituciju s aktivnim razmjenama.',
      updateSuccess: 'Institucija uspješno ažurirana.',
      addSuccess: 'Institucija uspješno dodana.'
    }
  },
  exchange: {
    title: 'Postavljanje razmjene',
    placeholder: 'Ova stranica je privremena i bit će implementirana uskoro.'
  },
  historyPage: {
    title: 'Povijest',
    placeholder: 'Ova stranica je privremena i bit će implementirana uskoro.'
  },
  callback: {
    signingIn: 'Prijava u tijeku...',
    failed: 'OAuth povratni poziv nije uspio.'
  },
  languageSwitcher: {
    dropdownLabel: 'Odaberi jezik',
    locales: {
      hr: 'Hrvatski',
      en: 'English'
    }
  },
  studyProgramLevels: {
    Undergraduate: 'Preddiplomski',
    Graduate: 'Diplomski',
    Postgraduate: 'Poslijediplomski'
  },
  courseTypes: {
    Mandatory: 'Obvezni predmet',
    CoreElective: 'Jezgreni predmet profila',
    ProfileElective: 'Izborni predmet profila',
    FreeElective: 'Izborni predmet',
    Transversal: 'Transverzalni predmet',
    Seminar: 'Seminar',
    ResearchSeminar: 'Istraživački seminar',
    Project: 'Projekt',
    Thesis: 'Diplomski rad'
  },
  courseStatuses: {
    Active: 'Aktivan',
    Inactive: 'Neaktivan',
    Historical: 'Arhiviran'
  },
  exchangeSemesters: {
    Winter: 'Zimski',
    Summer: 'Ljetni'
  }
}
