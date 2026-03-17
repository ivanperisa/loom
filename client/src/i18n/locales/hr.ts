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
        profileName: 'Naziv profila',
        level: 'Razina studijskog programa',
        durationSemesters: 'Trajanje (semestri)'
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
    jmbag: 'JMBAG',
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
    students: 'Studenti',
    history: 'Povijest'
  },
  settings: {
    title: 'Postavke',
    profile: {
      title: 'Profil',
      name: 'Ime',
      email: 'E-mail adresa',
      role: 'Uloga',
      jmbag: 'JMBAG',
      jmbagSaved: 'JMBAG uspješno spremljen.'
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
    title: 'Moja razmjena',
    noExchange: 'Nemate aktivne razmjene',
    create: 'Kreiraj razmjenu',
    submit: 'Predaj na pregled',
    academicYear: 'Akademska godina',
    semester: 'Semestar',
    duration: 'Trajanje (mjeseci)',
    mentor: 'Mentor',
    foreignInstitution: 'Strani fakultet',
    deleteConfirm: 'Jeste li sigurni da želite izbrisati ovu razmjenu?',
    deleteSuccess: 'Razmjena izbrisana.',
    submitConfirm: 'Jeste li sigurni da želite predati ovu razmjenu na pregled?',
    retract: 'Povuci prijavu',
    retractConfirm: 'Jeste li sigurni da želite povući razmjenu natrag u nacrt?',
    selectInstitution: '— odaberi —'
  },
  exchangeStatus: {
    Draft: 'Nacrt',
    Submitted: 'Predano',
    Approved: 'Odobreno',
    Rejected: 'Odbijeno',
    Completed: 'Završeno'
  },
  exchangeCourse: {
    add: 'Dodaj predmet',
    edit: 'Uredi predmet',
    remove: 'Ukloni predmet',
    removeConfirm: 'Jeste li sigurni da želite ukloniti ovaj predmet?',
    code: 'Šifra predmeta',
    nameEn: 'Naziv (engleski)',
    nameOriginal: 'Naziv (originalni jezik)',
    statusLabel: 'Status',
    ects: 'ECTS bodovi',
    hoursFormat: 'Sati (P/A/L)',
    lecturesHours: 'Predavanja',
    auditoryHours: 'Auditorne vježbe',
    labHours: 'Laboratorijske vježbe',
    originalGrade: 'Originalna ocjena',
    ectsGrade: 'ECTS ocjena',
    examDate: 'Datum ispita',
    addGrades: 'Unesi ocjene',
    saveGrades: 'Spremi ocjene'
  },
  exchangeCourseStatus: {
    OriginallyEnrolled: 'Izvorno upisan',
    Additional: 'Dodatni'
  },
  mapping: {
    title: 'Mapiranje',
    propose: 'Predloži mapiranje',
    addMapping: 'Dodaj mapiranje',
    awardedEcts: 'Priznato ECTS',
    convertedGrade: 'HR ocjena',
    coordinatorNote: 'Napomena koordinatora',
    ectsWarning: 'Suma ECTS-a premašuje ukupni ECTS stranog predmeta',
    history: 'Povijest',
    save: 'Spremi mapiranje',
    noMappings: 'Još nema predloženih mapiranja.',
    approve: 'Odobri',
    reject: 'Odbij'
  },
  mappingStatus: {
    Pending: 'Na čekanju',
    Approved: 'Odobreno',
    Rejected: 'Odbijeno'
  },
  mappingHistory: {
    title: 'Povijest mapiranja',
    noHistory: 'Nema zabilježenih promjena',
    changedBy: 'Promijenio'
  },
  historyPage: {
    title: 'Povijest mapiranja',
    selectStudent: 'Odaberi studenta',
    noStudents: 'Nema studenata s aktivnom razmjenom'
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
  },
  coordinator: {
    title: 'Moji studenti',
    noStudents: 'Nema dodijeljenih studenata',
    pendingReview: 'čekaju review',
    viewExchange: 'Pregled',
    backToList: 'Natrag na listu',
    approveExchange: 'Odobri razmjenu',
    rejectExchange: 'Odbij razmjenu',
    returnToDraft: 'Vrati na ispravak',
    confirmApprove: 'Jeste li sigurni da želite odobriti ovu razmjenu?',
    confirmReject: 'Jeste li sigurni da želite odbiti ovu razmjenu?',
    confirmReturn: 'Jeste li sigurni da želite vratiti razmjenu studentu na ispravak?',
    openMappingBoard: 'Otvori tablicu mapiranja',
    courseSummary: 'Sažetak predmeta',
    foreignCourses: 'stranih predmeta',
    mappingsCount: 'mapiranja',
    approved: 'odobreno',
    pending: 'na čekanju',
    rejected: 'odbijeno'
  },
  mappingBoard: {
    title: 'Tablica mapiranja',
    ferCourses: 'FER predmeti',
    foreignCourses: 'Strani predmeti',
    dropHere: 'Povuci FER predmet ovdje',
    noMappings: 'Još nema mapiranja',
    ectsWarning: 'Suma ECTS-a premašuje ukupni ECTS',
    unsavedChanges: 'Promjene nisu spremljene',
    proposeStudent: 'Predloži mapiranje',
    proposeCoordinator: 'Spremi pregled',
    mapped: '\u25cf mapirano'
  }
}
