export default {
  common: {
    appName: 'ExchangeMapper',
    signIn: 'Sign in with Google',
    signOut: 'Sign out',
    loading: 'Loading...',
    error: 'An error occurred.',
    na: 'N/A',
    language: 'Language',
    user: 'User',
    cancel: 'Cancel',
    confirm: 'Confirm'
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
    noExchange: 'You have no active exchange.',
    startExchange: 'Start New Exchange',
    institution: 'Home Institution',
    studyProgram: 'Study Program',
    studyProfile: 'Study Profile',
    viewExchange: 'View Exchange',
    coordinatorDashboard: 'Coordinator Dashboard',
    foreignInstitution: 'Foreign Institution',
    foreignProgram: 'Foreign Program',
    academicYear: 'Academic Year',
    semester: 'Semester',
    status: 'Status'
  },
  onboarding: {
    title: 'Account Setup',
    steps: {
      institution: 'Institution',
      jmbag: 'JMBAG'
    },
    selectInstitution: 'Select your home institution',
    selectInstitutionPlaceholder: '— select institution —',
    jmbagLabel: 'Enter your JMBAG',
    jmbagPlaceholder: '0036XXXXXX',
    jmbagHint: 'JMBAG must be exactly 10 digits.',
    submit: 'Complete setup',
    next: 'Next',
    back: 'Back',
    errors: {
      institutionRequired: 'Please select an institution before continuing.',
      jmbagRequired: 'JMBAG is required.',
      jmbagInvalid: 'JMBAG must be exactly 10 digits.'
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
    students: 'Students',
    history: 'History'
  },
  settings: {
    title: 'Settings',
    profile: {
      title: 'Profile',
      name: 'Name',
      email: 'Email address',
      role: 'Role',
      jmbag: 'JMBAG',
      jmbagSaved: 'JMBAG saved successfully.'
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
    mentor: 'Mentor',
    foreignInstitution: 'Foreign Institution',
    selectInstitution: '— select —',
    studySemester: 'Study Semester',
    noCoordinator: 'No coordinator',
    tabs: {
      learningAgreement: 'Learning Agreement',
      recognition: 'Recognition',
    },
    status: {
      waitingApproval: 'Waiting for coordinator approval',
    },
    actions: {
      submit: 'Submit for Review',
      backToDraft: 'Back to Draft',
      approve: 'Approve',
      reject: 'Reject',
    },
    coordinatorView: 'Coordinator view',
    student: 'Student',
  },
  createExchange: {
    title: 'New Exchange',
    steps: {
      program: 'Program & Profile',
      foreign: 'Foreign Program',
      details: 'Details',
      confirm: 'Confirm'
    },
    selectProgram: 'Select study program',
    selectProgramPlaceholder: '— select program —',
    selectProfile: 'Select profile',
    selectProfilePlaceholder: '— select profile —',
    selectForeignProgram: 'Select foreign program',
    selectCoordinator: 'Select coordinator',
    selectCoordinatorPlaceholder: '— optional —',
    academicYearPlaceholder: '2025/2026',
    studySemesterHint: 'Semester number (1–10)',
    mentorPlaceholder: 'Mentor full name',
    summary: 'Review selections',
    summaryProgram: 'Program',
    summaryProfile: 'Profile',
    summaryForeignProgram: 'Foreign Program',
    summaryCoordinator: 'Coordinator',
    summaryAcademicYear: 'Academic Year',
    summarySemesterType: 'Semester',
    summaryStudySemester: 'Study Semester',
    summaryMentor: 'Mentor',
    submitButton: 'Create Exchange',
    errors: {
      programRequired: 'Please select a study program.',
      profileRequired: 'Please select a profile.',
      foreignProgramRequired: 'Please select a foreign program.',
      academicYearRequired: 'Please enter an academic year.',
      studySemesterRequired: 'Please enter a study semester.'
    }
  },
  exchangeStatus: {
    Draft: 'Draft',
    Submitted: 'Submitted',
    Approved: 'Approved',
    Rejected: 'Rejected',
  },
  exchangeSemester: {
    Winter: 'Winter',
    Summer: 'Summer',
  },
  slotMode: {
    AtHome: 'At home institution',
    AtExchange: 'At exchange',
    AfterExchange: 'After exchange',
  },
  courseSlotCategory: {
    Mandatory: 'Mandatory',
    CoreElective: 'Core profile elective',
    ProfileElective: 'Profile elective',
    FreeElective: 'Free elective',
    Seminar: 'Seminar / Project',
    ResearchSeminar: 'Research seminar',
    Transversal: 'Transversal',
    Thesis: 'Thesis',
  },
  recognition: {
    notApproved: 'Recognition is available after the exchange is approved.',
    noEntries: 'No entries for recognition.',
    actions: {
      submit: 'Submit for Review',
      approve: 'Approve',
      reject: 'Reject',
    },
  },
  recognitionStatus: {
    Draft: 'Draft',
    Submitted: 'Submitted',
    Approved: 'Approved',
    Rejected: 'Rejected',
  },
  exchangeCourse: {
    add: 'Add Course',
    edit: 'Edit Course',
    remove: 'Remove Course',
    removeConfirm: 'Are you sure you want to remove this course?',
    code: 'Course Code',
    nameEn: 'Name (English)',
    nameOriginal: 'Name (original language)',
    statusLabel: 'Status',
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
  },
  coordinator: {
    title: 'My Students',
    noStudents: 'No assigned students',
    pendingReview: 'pending review',
    viewExchange: 'View',
    backToList: 'Back to list',
    approveExchange: 'Approve Exchange',
    rejectExchange: 'Reject Exchange',
    returnToDraft: 'Return for Revision',
    confirmApprove: 'Are you sure you want to approve this exchange?',
    confirmReject: 'Are you sure you want to reject this exchange?',
    confirmReturn: 'Return this exchange to the student for revision?',
    openMappingBoard: 'Open Mapping Board',
    courseSummary: 'Course Summary',
    foreignCourses: 'foreign courses',
    mappingsCount: 'mappings',
    approved: 'approved',
    pending: 'pending',
    rejected: 'rejected'
  },
  foreignCourses: {
    dragHint: 'Drag a course onto a cell marked as At exchange',
    addMapping: 'Add Mapping',
    availableEcts: 'Available ECTS',
    awardedEcts: 'Awarded ECTS',
    mappedCourses: 'Mapped courses',
    availableCourses: 'Available courses',
    allMapped: 'All courses have been fully mapped.',
    noMapped: 'No courses mapped yet.',
  },
  table: {
    semester: 'Semester',
    clickToChange: 'Click a cell to change status',
  },
  mappingBoard: {
    title: 'Mapping Board',
    ferCourses: 'FER Courses',
    foreignCourses: 'Foreign Courses',
    dropHere: 'Drag FER course here',
    noMappings: 'No mappings yet',
    ectsWarning: 'Awarded ECTS exceeds total',
    unsavedChanges: 'Unsaved changes',
    proposeStudent: 'Propose Mapping',
    proposeCoordinator: 'Save Review',
    mapped: '\u25cf mapped'
  }
}
