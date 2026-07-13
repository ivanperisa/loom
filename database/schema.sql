-- DROP (reverse dependency order)
DROP TABLE IF EXISTS exchange.snapshot                 CASCADE;
DROP TABLE IF EXISTS exchange.recognition_entry        CASCADE;
DROP TABLE IF EXISTS exchange.recognition              CASCADE;
DROP TABLE IF EXISTS exchange.mapping_scheme_entry     CASCADE;
DROP TABLE IF EXISTS exchange.learning_agreement_entry CASCADE;
DROP TABLE IF EXISTS exchange.learning_agreement       CASCADE;
DROP TABLE IF EXISTS exchange.exchange                 CASCADE;
DROP TABLE IF EXISTS partner.course                    CASCADE;
DROP TABLE IF EXISTS home.slot                         CASCADE;
DROP TABLE IF EXISTS home.course_group                 CASCADE;
DROP TABLE IF EXISTS home.course                       CASCADE;
DROP TABLE IF EXISTS home.profile                      CASCADE;
DROP TABLE IF EXISTS home.program                      CASCADE;
DROP TABLE IF EXISTS home.slot_type                    CASCADE;
DROP TABLE IF EXISTS public.coordinator_whitelist      CASCADE;
DROP TABLE IF EXISTS public."user"                     CASCADE;
DROP TABLE IF EXISTS public.institution                CASCADE;

DROP SCHEMA IF EXISTS exchange CASCADE;
DROP SCHEMA IF EXISTS partner  CASCADE;
DROP SCHEMA IF EXISTS home     CASCADE;

-- SCHEMAS
CREATE SCHEMA home;
CREATE SCHEMA partner;
CREATE SCHEMA exchange;

-- ============================================================
-- public
-- ============================================================

CREATE TABLE public.institution (
    id               SERIAL PRIMARY KEY,
    name             VARCHAR(255) NOT NULL,
    name_hr          VARCHAR(255),
    country          VARCHAR(100),
    city             VARCHAR(100),
    erasmus_code     VARCHAR(20),
    institution_type VARCHAR(10) NOT NULL DEFAULT 'Partner'
                         CHECK (institution_type IN ('Home', 'Partner')),
    is_deleted       BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at       TIMESTAMPTZ,
    created_at       TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE public."user" (
    id                         SERIAL PRIMARY KEY,
    external_id                VARCHAR(255) NOT NULL UNIQUE,
    email                      VARCHAR(255) NOT NULL,
    name                       VARCHAR(255) NOT NULL,
    jmbag                      VARCHAR(10),
    role                       VARCHAR(20) NOT NULL DEFAULT 'Student'
                                   CHECK (role IN ('Student', 'Coordinator', 'Admin')),
    is_onboarded               BOOLEAN NOT NULL DEFAULT FALSE,
    institution_id             INT REFERENCES public.institution(id) ON DELETE SET NULL,
    mentor                     VARCHAR(255),
    coordinator_id             INT REFERENCES public."user"(id) ON DELETE SET NULL,
    coordinator_request_status VARCHAR(20)
                                   CHECK (coordinator_request_status IN ('Pending', 'Rejected')),
    created_at                 TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at                 TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX idx_user_jmbag_not_null ON public."user"(jmbag) WHERE jmbag IS NOT NULL;
CREATE INDEX idx_user_email       ON public."user"(email);
CREATE INDEX idx_user_coordinator ON public."user"(coordinator_id);
CREATE INDEX idx_user_institution ON public."user"(institution_id);

CREATE TABLE public.coordinator_whitelist (
    id             SERIAL PRIMARY KEY,
    email          VARCHAR(255) NOT NULL UNIQUE,
    institution_id INT REFERENCES public.institution(id) ON DELETE SET NULL,
    created_at     TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ============================================================
-- home
-- ============================================================

CREATE TABLE home.slot_type (
    id         SERIAL PRIMARY KEY,
    name       VARCHAR(100) NOT NULL,
    name_en    VARCHAR(100),
    color      VARCHAR(7) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE home.program (
    id                 SERIAL PRIMARY KEY,
    institution_id     INT NOT NULL REFERENCES public.institution(id) ON DELETE CASCADE,
    name               VARCHAR(255) NOT NULL,
    name_en            VARCHAR(255),
    level              VARCHAR(20) NOT NULL
                           CHECK (level IN ('Graduate', 'Undergraduate', 'Postgraduate')),
    duration_semesters INT NOT NULL,
    created_at         TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX idx_home_program_institution ON home.program(institution_id);

CREATE TABLE home.profile (
    id         SERIAL PRIMARY KEY,
    program_id INT NOT NULL REFERENCES home.program(id) ON DELETE CASCADE,
    name       VARCHAR(255) NOT NULL,
    name_en    VARCHAR(255),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX idx_home_profile_program ON home.profile(program_id);

CREATE TABLE home.course (
    id         SERIAL PRIMARY KEY,
    isvu_code  INT NOT NULL,
    name       VARCHAR(255) NOT NULL,
    name_en    VARCHAR(255),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE UNIQUE INDEX idx_home_course_isvu ON home.course(isvu_code);

CREATE TABLE home.course_group (
    id           SERIAL PRIMARY KEY,
    slot_type_id INT NOT NULL REFERENCES home.slot_type(id),
    isvu_code    INT,
    name         VARCHAR(255) NOT NULL,
    name_en      VARCHAR(255),
    created_at   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX idx_home_course_group_slot_type ON home.course_group(slot_type_id);

CREATE TABLE home.slot (
    id              SERIAL PRIMARY KEY,
    profile_id      INT NOT NULL REFERENCES home.profile(id) ON DELETE CASCADE,
    semester        INT NOT NULL CHECK (semester BETWEEN 1 AND 4),
    slot_position   INT NOT NULL CHECK (slot_position BETWEEN 1 AND 30),
    ects            INT NOT NULL,
    slot_type_id    INT NOT NULL REFERENCES home.slot_type(id),
    course_id       INT REFERENCES home.course(id),
    course_group_id INT REFERENCES home.course_group(id),
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT chk_slot_exactly_one_source CHECK (
        (course_id IS NOT NULL AND course_group_id IS NULL) OR
        (course_id IS NULL AND course_group_id IS NOT NULL)
    )
);
CREATE INDEX idx_home_slot_profile ON home.slot(profile_id);

-- ============================================================
-- partner
-- ============================================================

CREATE TABLE partner.course (
    id             SERIAL PRIMARY KEY,
    institution_id INT NOT NULL REFERENCES public.institution(id) ON DELETE CASCADE,
    code           VARCHAR(50) NOT NULL,
    name           VARCHAR(255) NOT NULL,
    name_hr        VARCHAR(255),
    ects           NUMERIC(4, 1) NOT NULL,
    lectures_h     INT,
    auditory_h     INT,
    lab_h          INT,
    semester       VARCHAR(10) NOT NULL CHECK (semester IN ('Winter', 'Summer', 'Both')),
    level          VARCHAR(20) NOT NULL CHECK (level IN ('Graduate', 'Undergraduate', 'Postgraduate')),
    is_deleted     BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at     TIMESTAMPTZ,
    created_at     TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE UNIQUE INDEX idx_partner_course_institution_code ON partner.course(institution_id, code);

-- ============================================================
-- exchange
-- ============================================================

CREATE TABLE exchange.exchange (
    id                     SERIAL PRIMARY KEY,
    guid                   UUID NOT NULL DEFAULT gen_random_uuid() UNIQUE,
    student_id             INT NOT NULL REFERENCES public."user"(id),
    coordinator_id         INT REFERENCES public."user"(id) ON DELETE SET NULL,
    home_profile_id        INT NOT NULL REFERENCES home.profile(id),
    partner_institution_id INT NOT NULL REFERENCES public.institution(id),
    academic_year          VARCHAR(10) NOT NULL,
    semester_type          VARCHAR(10) NOT NULL CHECK (semester_type IN ('Winter', 'Summer', 'Both')),
    study_semesters        INT[] NOT NULL,
    coordinator_message    TEXT,
    ewp_link               VARCHAR(500),
    created_at             TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at             TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX idx_exchange_student             ON exchange.exchange(student_id);
CREATE INDEX idx_exchange_coordinator         ON exchange.exchange(coordinator_id);
CREATE INDEX idx_exchange_home_profile        ON exchange.exchange(home_profile_id);
CREATE INDEX idx_exchange_partner_institution ON exchange.exchange(partner_institution_id);

CREATE TABLE exchange.learning_agreement (
    id          SERIAL PRIMARY KEY,
    exchange_id INT NOT NULL UNIQUE REFERENCES exchange.exchange(id) ON DELETE CASCADE,
    status      VARCHAR(20) NOT NULL DEFAULT 'Draft'
                    CHECK (status IN ('Draft', 'Submitted', 'Approved', 'Rejected')),
    message     TEXT,
    approved_by INT REFERENCES public."user"(id) ON DELETE SET NULL,
    approved_at TIMESTAMPTZ,
    updated_by  INT REFERENCES public."user"(id) ON DELETE SET NULL,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX idx_la_exchange ON exchange.learning_agreement(exchange_id);
CREATE INDEX idx_la_status   ON exchange.learning_agreement(status);

CREATE TABLE exchange.learning_agreement_entry (
    id                    SERIAL PRIMARY KEY,
    learning_agreement_id INT NOT NULL REFERENCES exchange.learning_agreement(id) ON DELETE CASCADE,
    home_slot_id          INT NOT NULL REFERENCES home.slot(id),
    mode                  VARCHAR(20) NOT NULL CHECK (mode IN ('AtHome', 'AtExchange', 'AfterExchange')),
    partner_course_id     INT REFERENCES partner.course(id) ON DELETE SET NULL,
    awarded_ects          NUMERIC(4, 1),
    is_deleted            BOOLEAN NOT NULL DEFAULT FALSE,
    created_at            TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX idx_la_entry_la             ON exchange.learning_agreement_entry(learning_agreement_id);
CREATE INDEX idx_la_entry_slot           ON exchange.learning_agreement_entry(home_slot_id);
CREATE INDEX idx_la_entry_partner_course ON exchange.learning_agreement_entry(partner_course_id);

CREATE TABLE exchange.mapping_scheme_entry (
    id                      SERIAL PRIMARY KEY,
    exchange_id             INT NOT NULL REFERENCES exchange.exchange(id) ON DELETE CASCADE,
    home_slot_id            INT NOT NULL REFERENCES home.slot(id),
    partner_course_id       INT REFERENCES partner.course(id) ON DELETE SET NULL,
    awarded_ects            NUMERIC(4, 1),
    enrollment_status       VARCHAR(20) CHECK (enrollment_status IN ('Passed', 'NotPassed')),
    original_grade          VARCHAR(20),
    ects_grade              VARCHAR(5),
    hr_grade                VARCHAR(10),
    exam_date               DATE,
    is_recognized           BOOLEAN,
    recognized_as_course_id INT REFERENCES home.course(id) ON DELETE SET NULL,
    created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at              TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX idx_mapping_scheme_entry_exchange ON exchange.mapping_scheme_entry(exchange_id);
CREATE INDEX idx_mapping_scheme_entry_slot     ON exchange.mapping_scheme_entry(home_slot_id);

CREATE TABLE exchange.recognition (
    id          SERIAL PRIMARY KEY,
    exchange_id INT NOT NULL UNIQUE REFERENCES exchange.exchange(id) ON DELETE CASCADE,
    status      VARCHAR(20) NOT NULL DEFAULT 'Draft'
                    CHECK (status IN ('Draft', 'Submitted', 'Approved', 'Rejected')),
    message     TEXT,
    approved_by INT REFERENCES public."user"(id) ON DELETE SET NULL,
    approved_at TIMESTAMPTZ,
    updated_by  INT REFERENCES public."user"(id) ON DELETE SET NULL,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX idx_recognition_exchange ON exchange.recognition(exchange_id);

CREATE TABLE exchange.recognition_entry (
    id                          SERIAL PRIMARY KEY,
    recognition_id              INT NOT NULL REFERENCES exchange.recognition(id) ON DELETE CASCADE,
    learning_agreement_entry_id INT NOT NULL UNIQUE REFERENCES exchange.learning_agreement_entry(id),
    recognized_as_course_id     INT REFERENCES home.course(id) ON DELETE SET NULL,
    enrollment_status           VARCHAR(50),
    original_grade              VARCHAR(20),
    ects_grade                  VARCHAR(5),
    hr_grade                    VARCHAR(10),
    exam_date                   DATE,
    is_recognized               BOOLEAN,
    created_at                  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX idx_recognition_entry_recognition ON exchange.recognition_entry(recognition_id);
CREATE INDEX idx_recognition_entry_la_entry    ON exchange.recognition_entry(learning_agreement_entry_id);

CREATE TABLE exchange.snapshot (
    id            SERIAL PRIMARY KEY,
    exchange_id   INT NOT NULL REFERENCES exchange.exchange(id) ON DELETE CASCADE,
    changed_by_id INT NOT NULL REFERENCES public."user"(id),
    phase         VARCHAR(20) NOT NULL CHECK (phase IN ('LearningAgreement', 'Recognition')),
    type          VARCHAR(20) NOT NULL DEFAULT 'Auto' CHECK (type IN ('Auto', 'PreImport')),
    snapshot      JSONB NOT NULL,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX idx_snapshot_exchange         ON exchange.snapshot(exchange_id);
CREATE INDEX idx_snapshot_exchange_created ON exchange.snapshot(exchange_id, created_at DESC);
CREATE INDEX idx_snapshot_created          ON exchange.snapshot(created_at);