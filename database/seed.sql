﻿-- ============================================================
-- public.institution
-- ============================================================
INSERT INTO public.institution (id, name, country, city, erasmus_code, institution_type) VALUES
  (1, 'Fakultet elektrotehnike i računarstva', 'Hrvatska', 'Zagreb', NULL, 'Home');

SELECT setval(pg_get_serial_sequence('public.institution', 'id'), (SELECT MAX(id) FROM public.institution));

-- ============================================================
-- home.slot_type
-- ============================================================
INSERT INTO home.slot_type (id, name, name_en, color) VALUES
  (1, 'Obavezni predmeti',         'Mandatory course',        '#e2efda'),
  (2, 'Jezgreni kolegij profila',  'Core profile course',     '#c6e0b4'),
  (3, 'Izborni kolegij profila',  'Profile elective course', '#ffe699'),
  (4, 'Izborni kolegij',          'Free elective course',    '#fff2cc'),
  (5, 'Mentorski vođen rad',      'Mentor-guided work',      '#fce4d6'),
  (6, 'Istraživački seminar',     'Research seminar',        '#feebd1'),
  (7, 'Transverzalni kolegij',    'Transversal course',      '#ddebf7'),
  (8, 'Diplomski rad',            'Master thesis',           '#e7e6e6');

SELECT setval(pg_get_serial_sequence('home.slot_type', 'id'), (SELECT MAX(id) FROM home.slot_type));

-- ============================================================
-- home.program
-- ============================================================
INSERT INTO home.program (id, institution_id, name, name_en, level, duration_semesters) VALUES
  (10, 1, 'Računarstvo', 'Computer Science', 'Graduate', 4);

SELECT setval(pg_get_serial_sequence('home.program', 'id'), (SELECT MAX(id) FROM home.program));

-- ============================================================
-- home.profile
-- ============================================================
INSERT INTO home.profile (id, program_id, name, name_en) VALUES
  (101, 10, 'Programsko inženjerstvo i informacijski sustavi', 'Software Engineering and Information Systems'),
  (102, 10, 'Računalno inženjerstvo',                          'Computer Engineering'),
  (103, 10, 'Računarska znanost',                              'Computer Science'),
  (104, 10, 'Znanost o podacima',                              'Data Science'),
  (105, 10, 'Znanost o mrežama',                               'Network Science'),
  (106, 10, 'Računalno modeliranje u inženjerstvu',            'Computational Modelling in Engineering');

SELECT setval(pg_get_serial_sequence('home.profile', 'id'), (SELECT MAX(id) FROM home.profile));

-- ============================================================
-- home.course
-- ============================================================
INSERT INTO home.course (id, isvu_code, name, name_en) VALUES
  ( 1, 222496, 'Napredni algoritmi i strukture podataka', 'Advanced Algorithms and Data Structures'),
  ( 2, 284095, 'Diplomski projekt',                       'Graduation Project'),
  ( 3, 284107, 'Prezentacijski seminar',                  'Presentation Seminar'),
  ( 4, 252352, 'Napredne baze podataka',                  'Advanced Database Systems'),
  ( 5, 222547, 'Informacijski sustavi',                   'Information Systems'),
  ( 6, 222793, 'Sveprisutno računarstvo',                 'Ubiquitous Computing'),
  ( 7, 222786, 'Strojno učenje 1',                        'Machine Learning 1'),
  ( 8, 183376, 'Paralelizam i konkurentnost',             'Parallelism and Concurrency'),
  ( 9, 240719, 'Uvod u znanost o podacima',               'Introduction to Data Science'),
  (10, 183447, 'Osnove obradbe signala',                  'Signal Processing Fundamentals'),
  (11, 229840, 'Statistička analiza podataka',            'Statistical Data Analysis'),
  (12, 222664, 'Raspodijeljeni sustavi',                  'Distributed Systems'),
  (13, 222626, 'Komunikacijski protokoli',                'Communication Protocols'),
  (14, 222607, 'Jednadžbe matematičke fizike',            'Mathematical Physics Equations'),
  (15, 222559, 'Inženjerski dizajn',                      'Engineering Design'),
  (16, 183445, 'Numerička matematika',                    'Numerical Mathematics'),
  (17, 222549, 'Diplomski rad',                           'Master Thesis'),
  (18, 284098, 'Mentorski seminar',                       'Mentoring Seminar');

SELECT setval(pg_get_serial_sequence('home.course', 'id'), (SELECT MAX(id) FROM home.course));

-- ============================================================
-- home.course_group
-- ============================================================
INSERT INTO home.course_group (id, slot_type_id, isvu_code, name, name_en) VALUES
  (1,   1, NULL,   'Obavezni predmeti',       'Mandatory courses'),
  (101, 2, 18599,  'Jezgreni kolegij profila', 'Core profile course'),
  (102, 3, 18605,  'Izborni kolegij profila', 'Profile elective course'),
  (103, 4, 18760,  'Izborni kolegij',         'Free elective course'),
  (104, 4, 18715,  'Izborni kolegij',         'Free elective course'),
  (105, 7, 18602,  'Transverzalni kolegij',   'Transversal course'),
  (106, 7, 18603,  'Transverzalni kolegij',   'Transversal course'),
  (107, 7, 18604,  'Transverzalni kolegij',   'Transversal course'),
  (111, 2, 18608,  'Jezgreni kolegij profila', 'Core profile course'),
  (112, 3, 18615,  'Izborni kolegij profila', 'Profile elective course'),
  (113, 3, 18616,  'Izborni kolegij profila', 'Profile elective course'),
  (114, 3, 18617,  'Izborni kolegij profila', 'Profile elective course'),
  (115, 4, 18761,  'Izborni kolegij',         'Free elective course'),
  (116, 4, 18762,  'Izborni kolegij',         'Free elective course'),
  (117, 4, 18763,  'Izborni kolegij',         'Free elective course'),
  (118, 7, 18611,  'Transverzalni kolegij',   'Transversal course'),
  (119, 7, 18612,  'Transverzalni kolegij',   'Transversal course'),
  (120, 7, 18613,  'Transverzalni kolegij',   'Transversal course'),
  (131, 2, 18644,  'Jezgreni kolegij profila', 'Core profile course'),
  (132, 2, 18645,  'Jezgreni kolegij profila', 'Core profile course'),
  (133, 2, 18646,  'Jezgreni kolegij profila', 'Core profile course'),
  (134, 3, 18647,  'Izborni kolegij profila', 'Profile elective course'),
  (135, 3, 18648,  'Izborni kolegij profila', 'Profile elective course'),
  (136, 3, 18649,  'Izborni kolegij profila', 'Profile elective course'),
  (137, 4, 18773,  'Izborni kolegij',         'Free elective course'),
  (138, 4, 18774,  'Izborni kolegij',         'Free elective course'),
  (139, 4, 18775,  'Izborni kolegij',         'Free elective course'),
  (140, 7, 18651,  'Transverzalni kolegij',   'Transversal course'),
  (141, 7, 18652,  'Transverzalni kolegij',   'Transversal course'),
  (142, 7, 18653,  'Transverzalni kolegij',   'Transversal course'),
  (151, 3, 18625,  'Izborni kolegij profila', 'Profile elective course'),
  (152, 3, 18626,  'Izborni kolegij profila', 'Profile elective course'),
  (153, 4, 18764,  'Izborni kolegij',         'Free elective course'),
  (154, 4, 18765,  'Izborni kolegij',         'Free elective course'),
  (155, 4, 18766,  'Izborni kolegij',         'Free elective course'),
  (156, 7, 18620,  'Transverzalni kolegij',   'Transversal course'),
  (157, 7, 18621,  'Transverzalni kolegij',   'Transversal course'),
  (158, 7, 18622,  'Transverzalni kolegij',   'Transversal course'),
  (161, 2, 18634,  'Jezgreni kolegij profila', 'Core profile course'),
  (162, 2, 18635,  'Jezgreni kolegij profila', 'Core profile course'),
  (163, 3, 18627,  'Izborni kolegij profila', 'Profile elective course'),
  (164, 3, 18628,  'Izborni kolegij profila', 'Profile elective course'),
  (165, 3, 18629,  'Izborni kolegij profila', 'Profile elective course'),
  (166, 4, 18770,  'Izborni kolegij',         'Free elective course'),
  (167, 4, 18768,  'Izborni kolegij',         'Free elective course'),
  (168, 4, 18769,  'Izborni kolegij',         'Free elective course'),
  (169, 7, 18631,  'Transverzalni kolegij',   'Transversal course'),
  (170, 7, 18632,  'Transverzalni kolegij',   'Transversal course'),
  (171, 7, 18633,  'Transverzalni kolegij',   'Transversal course'),
  (181, 2, 18636,  'Jezgreni kolegij profila', 'Core profile course'),
  (182, 3, 18638,  'Izborni kolegij profila', 'Profile elective course'),
  (183, 3, 18639,  'Izborni kolegij profila', 'Profile elective course'),
  (184, 4, 18770,  'Izborni kolegij',         'Free elective course'),
  (185, 4, 18771,  'Izborni kolegij',         'Free elective course'),
  (186, 4, 18772,  'Izborni kolegij',         'Free elective course'),
  (187, 7, 18641,  'Transverzalni kolegij',   'Transversal course'),
  (188, 7, 18642,  'Transverzalni kolegij',   'Transversal course'),
  (189, 7, 18643,  'Transverzalni kolegij',   'Transversal course');

SELECT setval(pg_get_serial_sequence('home.course_group', 'id'), (SELECT MAX(id) FROM home.course_group));

-- ============================================================
-- home.slot
-- ============================================================
INSERT INTO home.slot (id, profile_id, semester, slot_position, ects, slot_type_id, course_id, course_group_id) VALUES
  -- PIIS sem1
  (200, 101, 1,  1, 5, 1,  1, NULL), (201, 101, 1,  6, 5, 1,  4, NULL), (202, 101, 1, 11, 5, 2, NULL, 101),
  (203, 101, 1, 16, 5, 3, NULL, 102), (204, 101, 1, 21, 5, 4, NULL, 103), (205, 101, 1, 26, 3, 1,  2, NULL),
  (206, 101, 1, 29, 2, 7, NULL, 105),
  -- PIIS sem2
  (207, 101, 2,  1, 5, 1,  5, NULL), (208, 101, 2,  6, 5, 2, NULL, 101), (209, 101, 2, 11, 5, 2, NULL, 101),
  (210, 101, 2, 16, 5, 3, NULL, 102), (211, 101, 2, 21, 5, 4, NULL, 104), (212, 101, 2, 26, 3, 5,  3, NULL),
  (213, 101, 2, 29, 2, 7, NULL, 106),
  -- PIIS sem3
  (214, 101, 3,  1, 5, 3, NULL, 102), (215, 101, 3,  6, 5, 3, NULL, 102), (216, 101, 3, 11, 5, 3, NULL, 102),
  (217, 101, 3, 16, 5, 4, NULL, 103), (218, 101, 3, 21, 5, 4, NULL, 103), (219, 101, 3, 26, 3, 5, 18, NULL),
  (220, 101, 3, 29, 2, 7, NULL, 107),
  -- PIIS sem4
  (221, 101, 4,  1, 30, 8, 17, NULL),
  -- RI sem1
  (222, 102, 1,  1, 5, 1,  1, NULL), (223, 102, 1,  6, 5, 2, NULL, 111), (224, 102, 1, 11, 5, 3, NULL, 112),
  (225, 102, 1, 16, 5, 4, NULL, 115), (226, 102, 1, 21, 5, 4, NULL, 115), (227, 102, 1, 26, 3, 1,  2, NULL),
  (228, 102, 1, 29, 2, 7, NULL, 118),
  -- RI sem2
  (229, 102, 2,  1, 5, 1,  6, NULL), (230, 102, 2,  6, 5, 2, NULL, 111), (231, 102, 2, 11, 5, 3, NULL, 113),
  (232, 102, 2, 16, 5, 4, NULL, 116), (233, 102, 2, 21, 5, 4, NULL, 116), (234, 102, 2, 26, 3, 5,  3, NULL),
  (235, 102, 2, 29, 2, 7, NULL, 119),
  -- RI sem3
  (236, 102, 3,  1, 5, 2, NULL, 111), (237, 102, 3,  6, 5, 3, NULL, 114), (238, 102, 3, 11, 5, 4, NULL, 117),
  (239, 102, 3, 16, 5, 4, NULL, 117), (240, 102, 3, 21, 5, 4, NULL, 117), (241, 102, 3, 26, 3, 5, 18, NULL),
  (242, 102, 3, 29, 2, 7, NULL, 120),
  -- RI sem4
  (243, 102, 4,  1, 30, 8, 17, NULL),
  -- RZ sem1
  (244, 103, 1,  1, 5, 1,  1, NULL), (245, 103, 1,  6, 5, 1,  7, NULL), (246, 103, 1, 11, 5, 2, NULL, 131),
  (247, 103, 1, 16, 5, 3, NULL, 134), (248, 103, 1, 21, 5, 4, NULL, 137), (249, 103, 1, 26, 3, 1,  2, NULL),
  (250, 103, 1, 29, 2, 7, NULL, 140),
  -- RZ sem2
  (251, 103, 2,  1, 5, 1,  8, NULL), (252, 103, 2,  6, 5, 2, NULL, 132), (253, 103, 2, 11, 5, 3, NULL, 135),
  (254, 103, 2, 16, 5, 3, NULL, 135), (255, 103, 2, 21, 5, 4, NULL, 138), (256, 103, 2, 26, 3, 5,  3, NULL),
  (257, 103, 2, 29, 2, 7, NULL, 141),
  -- RZ sem3
  (258, 103, 3,  1, 5, 2, NULL, 133), (259, 103, 3,  6, 5, 3, NULL, 136), (260, 103, 3, 11, 5, 3, NULL, 136),
  (261, 103, 3, 16, 5, 4, NULL, 139), (262, 103, 3, 21, 5, 4, NULL, 139), (263, 103, 3, 26, 3, 5, 18, NULL),
  (264, 103, 3, 29, 2, 7, NULL, 142),
  -- RZ sem4
  (265, 103, 4,  1, 30, 8, 17, NULL),
  -- ZoP sem1
  (266, 104, 1,  1, 5, 1, 11, NULL), (267, 104, 1,  6, 5, 1,  9, NULL), (268, 104, 1, 11, 5, 1, 10, NULL),
  (269, 104, 1, 16, 5, 1,  7, NULL), (270, 104, 1, 21, 5, 4, NULL, 153), (271, 104, 1, 26, 3, 1,  2, NULL),
  (272, 104, 1, 29, 2, 7, NULL, 156),
  -- ZoP sem2
  (273, 104, 2,  1, 5, 1, NULL,   1), (274, 104, 2,  6, 5, 3, NULL, 151), (275, 104, 2, 11, 5, 3, NULL, 151),
  (276, 104, 2, 16, 5, 3, NULL, 151), (277, 104, 2, 21, 5, 4, NULL, 154), (278, 104, 2, 26, 3, 5,  3, NULL),
  (279, 104, 2, 29, 2, 7, NULL, 157),
  -- ZoP sem3
  (280, 104, 3,  1, 5, 1,  1, NULL), (281, 104, 3,  6, 5, 3, NULL, 152), (282, 104, 3, 11, 5, 3, NULL, 152),
  (283, 104, 3, 16, 5, 4, NULL, 155), (284, 104, 3, 21, 5, 4, NULL, 155), (285, 104, 3, 26, 3, 5, 18, NULL),
  (286, 104, 3, 29, 2, 7, NULL, 158),
  -- ZoP sem4
  (287, 104, 4,  1, 30, 8, 17, NULL),
  -- ZoM sem1
  (288, 105, 1,  1, 5, 1,  1, NULL), (289, 105, 1,  6, 5, 1, 12, NULL), (290, 105, 1, 11, 5, 1, 13, NULL),
  (291, 105, 1, 16, 5, 3, NULL, 163), (292, 105, 1, 21, 5, 4, NULL, 166), (293, 105, 1, 26, 3, 1,  2, NULL),
  (294, 105, 1, 29, 2, 7, NULL, 169),
  -- ZoM sem2
  (295, 105, 2,  1, 5, 2, NULL, 161), (296, 105, 2,  6, 5, 2, NULL, 161), (297, 105, 2, 11, 5, 3, NULL, 164),
  (298, 105, 2, 16, 5, 3, NULL, 164), (299, 105, 2, 21, 5, 4, NULL, 167), (300, 105, 2, 26, 3, 5,  3, NULL),
  (301, 105, 2, 29, 2, 7, NULL, 170),
  -- ZoM sem3
  (302, 105, 3,  1, 5, 2, NULL, 162), (303, 105, 3,  6, 5, 3, NULL, 165), (304, 105, 3, 11, 5, 3, NULL, 165),
  (305, 105, 3, 16, 5, 4, NULL, 168), (306, 105, 3, 21, 5, 4, NULL, 168), (307, 105, 3, 26, 3, 5, 18, NULL),
  (308, 105, 3, 29, 2, 7, NULL, 171),
  -- ZoM sem4
  (309, 105, 4,  1, 30, 8, 17, NULL),
  -- RMI sem1
  (310, 106, 1,  1, 5, 1,  1, NULL), (311, 106, 1,  6, 5, 1, 14, NULL), (312, 106, 1, 11, 5, 1, 15, NULL),
  (313, 106, 1, 16, 5, 2, NULL, 181), (314, 106, 1, 21, 5, 4, NULL, 184), (315, 106, 1, 26, 3, 1,  2, NULL),
  (316, 106, 1, 29, 2, 7, NULL, 187),
  -- RMI sem2
  (317, 106, 2,  1, 5, 1, NULL,   1), (318, 106, 2,  6, 5, 1, NULL,   1), (319, 106, 2, 11, 5, 1, NULL,   1),
  (320, 106, 2, 16, 5, 3, NULL, 182), (321, 106, 2, 21, 5, 4, NULL, 185), (322, 106, 2, 26, 3, 5,  3, NULL),
  (323, 106, 2, 29, 2, 7, NULL, 188),
  -- RMI sem3
  (324, 106, 3,  1, 5, 1, NULL,   1), (325, 106, 3,  6, 5, 3, NULL, 183), (326, 106, 3, 11, 5, 3, NULL, 183),
  (327, 106, 3, 16, 5, 4, NULL, 186), (328, 106, 3, 21, 5, 4, NULL, 186), (329, 106, 3, 26, 3, 5, 18, NULL),
  (330, 106, 3, 29, 2, 7, NULL, 189),
  -- RMI sem4
  (331, 106, 4,  1, 30, 8, 17, NULL);

SELECT setval(pg_get_serial_sequence('home.slot', 'id'), (SELECT MAX(id) FROM home.slot));