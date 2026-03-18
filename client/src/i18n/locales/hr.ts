export default {
  common: {
    appName: 'ExchangeMapper',
    signIn: 'Prijava putem Googlea',
    signOut: 'Odjava',
    loading: 'Učitavanje...',
    error: 'Došlo je do greške.',
    na: 'N/A',
    language: 'Jezik',
    user: 'Korisnik',
    cancel: 'Odustani',
    confirm: 'Potvrdi'
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
    noExchange: 'Nemate aktivnu razmjenu.',
    startExchange: 'Pokreni novu razmjenu',
    institution: 'Matični fakultet',
    studyProgram: 'Studijski program',
    studyProfile: 'Studijski profil',
    viewExchange: 'Pregledaj razmjenu',
    coordinatorDashboard: 'Koordinatorska ploča',
    foreignInstitution: 'Strani fakultet',
    foreignProgram: 'Strani program',
    academicYear: 'Akademska godina',
    semester: 'Semestar',
    status: 'Status'
  },
  onboarding: {
    title: 'Postavljanje računa',
    steps: {
      institution: 'Institucija',
      jmbag: 'JMBAG'
    },
    selectInstitution: 'Odaberite matični fakultet',
    selectInstitutionPlaceholder: '— odaberite instituciju —',
    jmbagLabel: 'Unesite JMBAG',
    jmbagPlaceholder: '0036XXXXXX',
    jmbagHint: 'JMBAG mora imati točno 10 znamenki.',
    submit: 'Završi postavljanje',
    next: 'Sljedeći',
    back: 'Natrag',
    errors: {
      institutionRequired: 'Odaberite instituciju prije nastavka.',
      jmbagRequired: 'JMBAG je obavezan.',
      jmbagInvalid: 'JMBAG mora sadržavati točno 10 znamenki.'
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
    mentor: 'Mentor',
    foreignInstitution: 'Strani fakultet',
    selectInstitution: '— odaberi —',
    studySemester: 'Semestar studija',
    noCoordinator: 'Bez koordinatora',
    tabs: {
      learningAgreement: 'Ugovor o učenju',
      recognition: 'Priznavanje',
    },
    status: {
      waitingApproval: 'Čeka odobrenje koordinatora',
    },
    actions: {
      submit: 'Predaj na pregled',
      backToDraft: 'Vrati u nacrt',
      approve: 'Odobri',
      reject: 'Odbij',
    },
    coordinatorView: 'Koordinatorski pregled',
    student: 'Student',
  },
  createExchange: {
    title: 'Nova razmjena',
    steps: {
      program: 'Program i profil',
      foreign: 'Strani program',
      details: 'Detalji',
      confirm: 'Potvrda'
    },
    selectProgram: 'Odaberite studijski program',
    selectProgramPlaceholder: '— odaberite program —',
    selectProfile: 'Odaberite profil',
    selectProfilePlaceholder: '— odaberite profil —',
    selectForeignProgram: 'Odaberite strani program',
    selectCoordinator: 'Odaberite koordinatora',
    selectCoordinatorPlaceholder: '— opcija —',
    academicYearPlaceholder: '2025/2026',
    studySemesterHint: 'Redni broj semestra (1–10)',
    mentorPlaceholder: 'Ime i prezime mentora',
    summary: 'Pregled odabira',
    summaryProgram: 'Program',
    summaryProfile: 'Profil',
    summaryForeignProgram: 'Strani program',
    summaryCoordinator: 'Koordinator',
    summaryAcademicYear: 'Ak. godina',
    summarySemesterType: 'Semestar',
    summaryStudySemester: 'Semestar studija',
    summaryMentor: 'Mentor',
    submitButton: 'Kreiraj razmjenu',
    errors: {
      programRequired: 'Odaberite studijski program.',
      profileRequired: 'Odaberite profil.',
      foreignProgramRequired: 'Odaberite strani program.',
      academicYearRequired: 'Unesite akademsku godinu.',
      studySemesterRequired: 'Unesite semestar studija.'
    }
  },
  exchangeStatus: {
    Draft: 'Nacrt',
    Submitted: 'Predano',
    Approved: 'Odobreno',
    Rejected: 'Odbijeno',
  },
  exchangeSemester: {
    Winter: 'Zimski',
    Summer: 'Ljetni',
  },
  slotMode: {
    AtHome: 'Na matičnom fakultetu',
    AtExchange: 'Na razmjeni',
    AfterExchange: 'Nakon razmjene',
  },
  courseSlotCategory: {
    Mandatory: 'Obavezni predmet',
    CoreElective: 'Jezgreni predmet profila',
    ProfileElective: 'Izborni predmet profila',
    FreeElective: 'Izborni predmet',
    Seminar: 'Seminar / Projekt',
    ResearchSeminar: 'Istraživački seminar',
    Transversal: 'Transverzalni predmet',
    Thesis: 'Diplomski rad',
  },
  recognition: {
    notApproved: 'Priznavanje je dostupno tek nakon odobrenja razmjene.',
    noEntries: 'Nema stavki za priznavanje.',
    actions: {
      submit: 'Predaj na pregled',
      approve: 'Odobri',
      reject: 'Odbij',
    },
  },
  recognitionStatus: {
    Draft: 'Nacrt',
    Submitted: 'Predano',
    Approved: 'Odobreno',
    Rejected: 'Odbijeno',
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
  foreignCourses: {
    dragHint: 'Povuci predmet na ćeliju označenu kao Na razmjeni',
    addMapping: 'Dodaj mapiranje',
    availableEcts: 'Dostupno ECTS',
    awardedEcts: 'Dodijeljeno ECTS',
    mappedCourses: 'Mapirani predmeti',
    availableCourses: 'Dostupni predmeti',
    allMapped: 'Svi predmeti su u potpunosti mapirani.',
    noMapped: 'Još nema mapiranih predmeta.',
  },
  table: {
    semester: 'Semestar',
    clickToChange: 'Kliknite na ćeliju za promjenu statusa',
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
