export default {
  common: {
    appName: 'ExchangeMapper',
    signIn: 'Sign in with Google',
    signOut: 'Sign out',
    loading: 'Loading...',
    error: 'An error occurred.',
    na: 'N/A',
    language: 'Language',
    user: 'User'
  },
  auth: {
    signInWithGoogle: 'Sign in with Google'
  },
  landing: {
    tagline: 'Simplify your Erasmus course mapping',
    description: 'Map your exchange courses, track ECTS credits, and get approval from your coordinator - all in one place.',
    visualTitle: 'Course Mapping Made Simple'
  },
  home: {
    welcome: 'Welcome back, {name}',
    noExchange: 'You have no active exchange. Get started below.',
    startExchange: 'Start Exchange Setup',
    institution: 'Home Institution',
    studyProgram: 'Study Program',
    studyProfile: 'Study Profile'
  },
  onboarding: {
    title: 'Account Setup',
    steps: {
      role: 'Role',
      institutions: 'Institutions',
      confirm: 'Confirm'
    },
    role: {
      title: 'Choose your role',
      student: 'Student',
      coordinator: 'Coordinator',
      coordinatorNote: 'Your role will be confirmed.'
    },
    institutions: {
      title: 'Select your institutions',
      subtitle: 'Add all universities where you study.',
      addToList: 'Add to list',
      added: 'Added institutions',
      empty: 'You have not added any institutions.',
      minOne: 'Add at least one institution to continue.',
      duplicate: 'This institution and profile combination is already added.',
      existingOption: 'Existing institution',
      newOption: 'New institution',
      searchPlaceholder: 'Search institutions...',
      selectProgram: 'Select program',
      selectProfile: 'Select profile',
      newProfileOption: '+ New profile',
      newProfilePlaceholder: 'New profile name',
      form: {
        name: 'Institution name',
        country: 'Country',
        city: 'City',
        erasmusCode: 'Erasmus code',
        iscedCode: 'ISCED code',
        programName: 'Program name',
        profileName: 'Profile name'
      }
    },
    confirm: {
      title: 'Review your details',
      submit: 'Complete setup',
      role: 'Role',
      institution: 'Institution',
      program: 'Program',
      profile: 'Profile'
    },
    next: 'Next',
    back: 'Back',
    noResults: 'Not found.',
    errors: {
      selectRole: 'Choose your role before continuing.',
      institutionRequired: 'Select an institution or enter details for a new one.',
      programProfileRequired: 'Select a study program and profile or add a new profile.'
    }
  },
  errors: {
    required: 'This field is required.',
    notFound: 'Not found.',
    unexpected: 'An unexpected error occurred.',
    unauthorized: 'You are not authorized to view this page.'
  },
  nav: {
    home: 'Home',
    settings: 'Settings',
    exchange: 'Exchange',
    history: 'History'
  },
  settings: {
    title: 'Settings',
    profile: {
      title: 'Profile',
      name: 'Name',
      email: 'Email address',
      role: 'Role'
    },
    institutions: {
      title: 'My institutions',
      empty: 'You have no institutions added.',
      program: 'Program',
      profile: 'Profile',
      edit: 'Edit',
      editDisabledTooltip: 'Cannot edit institution with active exchanges.',
      save: 'Save',
      cancel: 'Cancel',
      remove: 'Remove',
      addButton: 'Add institution',
      addTitle: 'Add institution',
      removeConfirm: 'Are you sure you want to remove this institution?',
      removeError: 'Cannot remove institution with active exchanges.',
      updateSuccess: 'Institution updated successfully.',
      addSuccess: 'Institution added successfully.'
    }
  },
  exchange: {
    title: 'My Exchange',
    noExchange: 'No active exchange',
    create: 'Create Exchange',
    submit: 'Submit for Review',
    academicYear: 'Academic Year',
    semester: 'Semester',
    duration: 'Duration (months)',
    mentor: 'Mentor',
    foreignInstitution: 'Foreign Institution',
    deleteConfirm: 'Are you sure you want to delete this exchange?',
    deleteSuccess: 'Exchange deleted.',
    submitConfirm: 'Are you sure you want to submit this exchange for review?',
    retract: 'Retract',
    retractConfirm: 'Are you sure you want to retract this exchange back to draft?'
  },
  exchangeStatus: {
    Draft: 'Draft',
    Submitted: 'Submitted',
    Approved: 'Approved',
    Rejected: 'Rejected',
    Completed: 'Completed'
  },
  exchangeCourse: {
    add: 'Add Course',
    edit: 'Edit Course',
    remove: 'Remove Course',
    removeConfirm: 'Are you sure you want to remove this course?',
    code: 'Course Code',
    nameEn: 'Name (English)',
    nameHr: 'Name (Croatian)',
    ects: 'ECTS Credits',
    hoursFormat: 'Hours (L/A/Lab)',
    lecturesHours: 'Lectures',
    auditoryHours: 'Auditory',
    labHours: 'Lab',
    originalGrade: 'Original Grade',
    ectsGrade: 'ECTS Grade',
    examDate: 'Exam Date',
    addGrades: 'Enter Grades',
    saveGrades: 'Save Grades'
  },
  exchangeCourseStatus: {
    OriginallyEnrolled: 'Originally Enrolled',
    Additional: 'Additional'
  },
  mapping: {
    title: 'Mapping',
    propose: 'Propose Mapping',
    addMapping: 'Add Mapping',
    awardedEcts: 'Awarded ECTS',
    convertedGrade: 'Croatian Grade',
    coordinatorNote: 'Coordinator Note',
    ectsWarning: 'Awarded ECTS exceeds total course ECTS',
    history: 'History',
    save: 'Save Mapping',
    noMappings: 'No mappings proposed yet.',
    approve: 'Approve',
    reject: 'Reject'
  },
  mappingStatus: {
    Pending: 'Pending',
    Approved: 'Approved',
    Rejected: 'Rejected'
  },
  mappingHistory: {
    title: 'Mapping History',
    noHistory: 'No changes recorded',
    changedBy: 'Changed by'
  },
  historyPage: {
    title: 'Mapping History',
    selectStudent: 'Select student',
    noStudents: 'No students with an active exchange'
  },
  callback: {
    signingIn: 'Signing in...',
    failed: 'OAuth callback failed.'
  },
  languageSwitcher: {
    dropdownLabel: 'Select language',
    locales: {
      hr: 'Croatian',
      en: 'English'
    }
  },
  studyProgramLevels: {
    Undergraduate: 'Undergraduate',
    Graduate: 'Graduate',
    Postgraduate: 'Postgraduate'
  },
  courseTypes: {
    Mandatory: 'Mandatory',
    CoreElective: 'Core profile elective',
    ProfileElective: 'Profile elective',
    FreeElective: 'Free elective',
    Transversal: 'Transversal',
    Seminar: 'Seminar',
    ResearchSeminar: 'Research seminar',
    Project: 'Project',
    Thesis: 'Thesis'
  },
  courseStatuses: {
    Active: 'Active',
    Inactive: 'Inactive',
    Historical: 'Historical'
  },
  exchangeSemesters: {
    Winter: 'Winter',
    Summer: 'Summer'
  }
}
