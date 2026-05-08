import XLSX from 'xlsx-js-style'
import { exchangeSemester } from '@/utils/exchangeSemester'
import type { RecognitionResponse } from '@/types/recognition.types'
import type { ExchangeResponse, LearningAgreementResponse, LearningAgreementEntryResponse } from '@/types/exchange.types'

// Style helpers

type XlsxCell = { v?: string | number; t?: string; s?: object; l?: object }

const FONT = 'Calibri'
const HEADER_BG = 'D9D9D9'
const RED_ROW_BG = 'FFCCCC'

function thin() { return { style: 'thin', color: { rgb: 'BFBFBF' } } }
function border() { return { top: thin(), bottom: thin(), left: thin(), right: thin() } }
function noBorder() { return { top: { style: 'none' }, bottom: { style: 'none' }, left: { style: 'none' }, right: { style: 'none' } } }

function c(
  value: string | number | null | undefined,
  opts: {
    bg?: string; bold?: boolean; sz?: number; wrap?: boolean
    halign?: 'left' | 'center' | 'right'; valign?: 'top' | 'middle' | 'bottom'
    color?: string; borders?: boolean; italic?: boolean; indent?: number
    underline?: boolean
  } = {},
): XlsxCell {
  const {
    bg, bold = false, sz = 9, wrap = false, halign = 'left', valign = 'middle',
    color = '000000', borders = true, italic = false, indent = 0, underline = false,
  } = opts
  return {
    v: value ?? '',
    t: typeof value === 'number' ? 'n' : 's',
    s: {
      font: { name: FONT, sz, bold, italic, underline, color: { rgb: color } },
      alignment: { wrapText: wrap, horizontal: halign, vertical: valign, indent },
      ...(bg ? { fill: { fgColor: { rgb: bg.replace('#', '') } } } : {}),
      ...(borders ? { border: border() } : { border: noBorder() }),
    },
  }
}

function link(value: string, url: string, bg?: string): XlsxCell {
  const cell = c(value, { color: '0563C1', underline: true, bg, borders: true, wrap: true }) as XlsxCell
  if (url) cell.l = { Target: url }
  return cell
}

function empty(bg?: string, borders = true): XlsxCell { return c('', { bg, borders }) }

function colLetter(idx: number): string {
  let s = ''; let n = idx + 1
  while (n > 0) { const r = (n - 1) % 26; s = String.fromCharCode(65 + r) + s; n = Math.floor((n - 1) / 26) }
  return s
}

// Translation map

type Lang = 'hr' | 'en' | string

const T: Record<string, Record<Lang, string>> = {
  title:            { hr: 'ISVU-obrazac za priznavanje predmeta ERASMUS-studentima', en: 'ISVU-form for course recognition — Erasmus students' },
  student:          { hr: 'Student:', en: 'Student:' },
  jmbag:            { hr: 'JMBAG:', en: 'JMBAG:' },
  studyType:        { hr: 'Studij (prediplomski/diplomski):', en: 'Study (undergraduate/graduate):' },
  studyTypeVal:     { hr: 'diplomski', en: 'graduate' },
  semester:         { hr: 'Semestar:', en: 'Semester:' },
  profile:          { hr: 'Profil (za diplomski):', en: 'Profile (graduate):' },
  university:       { hr: 'Sveučilište razmjene:', en: 'Exchange university:' },
  faculty:          { hr: 'Fakultet razmjene:', en: 'Exchange faculty:' },
  academicYear:     { hr: 'Ak. god. razmjene:', en: 'Academic year:' },
  exchSemester:     { hr: 'Semestar razmjene (zimski/ljetni):', en: 'Exchange semester (winter/summer):' },
  mentor:           { hr: 'Mentor:', en: 'Mentor:' },
  winter:           { hr: 'zimski', en: 'winter' },
  summer:           { hr: 'ljetni', en: 'summer' },
  sectionTitle:     { hr: 'Predmeti koji se priznaju za druge predmete/obveze iz nastavnog programa', en: 'Courses recognized towards programme obligations' },
  ukupno:           { hr: 'UKUPNO', en: 'TOTAL' },
  profileLabel:     { hr: 'Profil:', en: 'Profile:' },
  napomeneTitle:    { hr: 'NAPOMENE:', en: 'NOTES:' },
  napomene1:        { hr: 'U Learning Agreement, tablica B stavlja KATEGORIJE PREDMETA, ne pojedine predmete!', en: 'In the Learning Agreement, Table B lists COURSE CATEGORIES, not individual courses!' },
  napomene2:        { hr: 'Za jezgrene i obvezne predmete mora biti 1:1 zamjena te se mora u tablici mapiranja navesti ime predmeta za kojeg se priznaje!', en: 'Core and mandatory courses require a 1:1 substitution — the course being substituted must be listed!' },
  napomene3:        { hr: 'Poveznice zamijeniti stvarnim poveznicama na strane/domaće kolegije', en: 'Replace links with actual links to foreign/domestic courses' },
  sheetRecognition: { hr: 'Priznavanje', en: 'Recognition' },
  sheetLA:          { hr: 'Ugovor o učenju', en: 'Learning Agreement' },
  colForeignCode:   { hr: 'Šifra predmeta', en: 'Course Code' },
  colNameEn:        { hr: 'Naziv engleski', en: 'Name (English)' },
  colStatus:        { hr: 'Status predmeta', en: 'Course Status' },
  colNameHr:        { hr: 'Naziv - hrvatski', en: 'Name (Croatian)' },
  colHours:         { hr: 'Sati u obliku:\nPredavanja/Auditorne/\nlaboratorijske vježbe (P/A/L)', en: 'Hours:\nLectures/Auditory/\nLaboratory (P/A/L)' },
  colEcts:          { hr: 'ECTS', en: 'ECTS' },
  colRbr:           { hr: 'Rbr.', en: 'No.' },
  colRecognizedAs:  { hr: 'Priznaje se za predmet', en: 'Recognized as' },
  colSlotName:      { hr: 'Naziv', en: 'Name' },
  colSlotCode:      { hr: 'Izb. grupa', en: 'Elective group' },
  colSlotCategory:  { hr: 'Naziv izb. grupe', en: 'Elective group name' },
  colSemester:      { hr: 'Semestar', en: 'Semester' },
  colAwarded:       { hr: 'Priznato ECTS-a', en: 'Awarded ECTS' },
  colOrigGrade:     { hr: 'Ocjena\noriginalna', en: 'Original\nGrade' },
  colEctsGrade:     { hr: 'Ocjena\nECTS\n(F-A)', en: 'ECTS\nGrade\n(F-A)' },
  colHrGrade:       { hr: 'Ocjena\nhrv.\n(1-5)', en: 'Croatian\nGrade\n(1-5)' },
  colDate:          { hr: 'Datum polaganja', en: 'Exam Date' },
  laAtHome:         { hr: 'položeno na FER-u', en: 'Taken at home institution' },
  laAtExchange:     { hr: 'polaže na mobilnosti', en: 'Taken on exchange' },
  laAfterExchange:  { hr: 'ostaje nakon mobilnosti', en: 'Taken after exchange' },
}

function tr(key: string, lang: Lang): string {
  return T[key]?.[lang] ?? T[key]?.['hr'] ?? key
}

// Sheet 1: Recognition

function buildRecognitionSheet(
  recognition: RecognitionResponse,
  exchange: ExchangeResponse,
  lang: Lang,
): Record<string, XlsxCell> {
  const ws: XLSX.WorkSheet = {}
  const merges: XLSX.Range[] = []
  const LAST_COL = 16  // Q (0-indexed), cols A–Q = 0–16

  // Row 1: title
  ws['A1'] = c(tr('title', lang), { bold: true, sz: 11, borders: false })
  for (let ci = 1; ci <= LAST_COL; ci++) ws[`${colLetter(ci)}1`] = empty(undefined, false)
  merges.push({ s: { r: 0, c: 0 }, e: { r: 0, c: LAST_COL } })

  // Row 2: spacer
  for (let ci = 0; ci <= LAST_COL; ci++) ws[`${colLetter(ci)}2`] = empty(undefined, false)

  // Helper: info row, label right-aligned in E-H, value in I-Q, A-D blank
  function infoRow(row: number, label: string, value: string | null, labelColor?: string) {
    for (let ci = 0; ci <= 3; ci++) ws[`${colLetter(ci)}${row}`] = empty(undefined, false)
    merges.push({ s: { r: row - 1, c: 0 }, e: { r: row - 1, c: 3 } })

    ws[`E${row}`] = c(label, { bold: true, halign: 'right', borders: false, color: labelColor })
    for (let ci = 5; ci <= 7; ci++) ws[`${colLetter(ci)}${row}`] = empty(undefined, false)
    merges.push({ s: { r: row - 1, c: 4 }, e: { r: row - 1, c: 7 } })

    ws[`I${row}`] = c(value ?? '', { borders: false })
    for (let ci = 9; ci <= LAST_COL; ci++) ws[`${colLetter(ci)}${row}`] = empty(undefined, false)
    merges.push({ s: { r: row - 1, c: 8 }, e: { r: row - 1, c: LAST_COL } })
  }

  infoRow(3, tr('student', lang), exchange.studentName)
  infoRow(4, tr('jmbag', lang), exchange.studentJmbag)
  infoRow(5, tr('studyType', lang), tr('studyTypeVal', lang))
  infoRow(6, tr('semester', lang), String(exchange.studySemester))

  // Rows 7-12: left = big profile block (A7:D12 merged), right = more info
  ws['A7'] = c(`${tr('profileLabel', lang)}  ${exchange.studyProfile.name}`, {
    bold: true, sz: 16, borders: false, valign: 'middle', wrap: true,
  })
  merges.push({ s: { r: 6, c: 0 }, e: { r: 11, c: 3 } })
  // Fill phantom cells for the merged block rows 8–12
  for (let row = 8; row <= 12; row++) {
    for (let ci = 0; ci <= 3; ci++) ws[`${colLetter(ci)}${row}`] = empty(undefined, false)
  }

  // Helper: right-side info row for rows 7–12 (no A–D merge, those are in the big block)
  function infoRowRight(row: number, label: string, value: string | null, labelColor?: string) {
    ws[`E${row}`] = c(label, { bold: true, halign: 'right', borders: false, color: labelColor })
    for (let ci = 5; ci <= 7; ci++) ws[`${colLetter(ci)}${row}`] = empty(undefined, false)
    merges.push({ s: { r: row - 1, c: 4 }, e: { r: row - 1, c: 7 } })

    ws[`I${row}`] = c(value ?? '', { borders: false })
    for (let ci = 9; ci <= LAST_COL; ci++) ws[`${colLetter(ci)}${row}`] = empty(undefined, false)
    merges.push({ s: { r: row - 1, c: 8 }, e: { r: row - 1, c: LAST_COL } })
  }

  infoRowRight(7,  tr('profile', lang),     exchange.studyProfile.name)
  // University is a hyperlink, use foreignProgram.url if available, else plain text
  const uniUrl = (exchange.foreignProgram as any).url ?? ''
  if (uniUrl) {
    ws[`E8`] = c(tr('university', lang), { bold: true, halign: 'right', borders: false, color: 'FF0000' })
    for (let ci = 5; ci <= 7; ci++) ws[`${colLetter(ci)}8`] = empty(undefined, false)
    merges.push({ s: { r: 7, c: 4 }, e: { r: 7, c: 7 } })
    ws[`I8`] = link(exchange.foreignProgram.institutionName, uniUrl)
    for (let ci = 9; ci <= LAST_COL; ci++) ws[`${colLetter(ci)}8`] = empty(undefined, false)
    merges.push({ s: { r: 7, c: 8 }, e: { r: 7, c: LAST_COL } })
  } else {
    infoRowRight(8, tr('university', lang), exchange.foreignProgram.institutionName, 'FF0000')
  }
  infoRowRight(9,  tr('faculty', lang),     exchange.foreignProgram.name)
  infoRowRight(10, tr('academicYear', lang), exchange.academicYear)
  infoRowRight(11, tr('exchSemester', lang), exchange.semesterType === exchangeSemester.Winter ? tr('winter', lang) : tr('summer', lang))
  infoRowRight(12, tr('mentor', lang),      exchange.mentor)

  // Row 13: section subtitle (italic red)
  ws['A13'] = c(tr('sectionTitle', lang), { sz: 9, italic: true, color: 'FF0000', borders: false })
  for (let ci = 1; ci <= LAST_COL; ci++) ws[`${colLetter(ci)}13`] = empty(undefined, false)
  merges.push({ s: { r: 12, c: 0 }, e: { r: 12, c: LAST_COL } })

  // Rows 14-15: spacers
  for (let row = 14; row <= 15; row++)
    for (let ci = 0; ci <= LAST_COL; ci++) ws[`${colLetter(ci)}${row}`] = empty(undefined, false)

  // Rows 16-17: column headers
  const hdr = (v: string) => c(v, { bold: true, bg: HEADER_BG, wrap: true, halign: 'center', valign: 'middle' })

  // Row 16 super-headers
  ws['A16'] = hdr(tr('colForeignCode', lang))
  ws['B16'] = hdr(tr('colNameEn', lang))
  ws['C16'] = hdr(tr('colStatus', lang))
  ws['D16'] = hdr(tr('colNameHr', lang))
  ws['E16'] = hdr(tr('colHours', lang))
  ws['F16'] = hdr(tr('colEcts', lang))
  ws['G16'] = hdr(tr('colRbr', lang))
  // H–M: "Priznaje se za predmet" super-header, merged across H16:M16
  ws['H16'] = hdr(tr('colRecognizedAs', lang))
  for (let ci = 8; ci <= 12; ci++) ws[`${colLetter(ci)}16`] = hdr('')
  merges.push({ s: { r: 15, c: 7 }, e: { r: 15, c: 12 } })
  ws['N16'] = hdr(tr('colOrigGrade', lang))
  ws['O16'] = hdr(tr('colEctsGrade', lang))
  ws['P16'] = hdr(tr('colHrGrade', lang))
  ws['Q16'] = hdr(tr('colDate', lang))

  // Rowspan A–G and N–Q: merge row16 with row17
  for (const ci of [0, 1, 2, 3, 4, 5, 6, 13, 14, 15, 16]) {
    merges.push({ s: { r: 15, c: ci }, e: { r: 16, c: ci } })
  }

  // Row 17: phantom cells for rowspan cols, sub-headers for H-M
  ws['A17'] = hdr('')
  ws['B17'] = hdr('')
  ws['C17'] = hdr('')
  ws['D17'] = hdr('')
  ws['E17'] = hdr('')
  ws['F17'] = hdr('')
  ws['G17'] = hdr('')
  ws['H17'] = hdr(tr('colSlotName', lang))
  ws['I17'] = hdr('')
  merges.push({ s: { r: 16, c: 7 }, e: { r: 16, c: 8 } })  // H17:I17
  ws['J17'] = hdr(tr('colSlotCode', lang))
  ws['K17'] = hdr(tr('colSlotCategory', lang))
  ws['L17'] = hdr(tr('colSemester', lang))
  ws['M17'] = hdr(tr('colAwarded', lang))
  ws['N17'] = hdr('')
  ws['O17'] = hdr('')
  ws['P17'] = hdr('')
  ws['Q17'] = hdr('')

  // Data rows (from row 18), no spacer rows between groups
  const groups = new Map<string, typeof recognition.entries>()
  for (const entry of recognition.entries) {
    if (!groups.has(entry.foreignCourseCode)) groups.set(entry.foreignCourseCode, [])
    groups.get(entry.foreignCourseCode)!.push(entry)
  }

  let row = 18
  const categoryTotals = new Map<string, { name: string; color: string; ects: number }>()

  for (const [, entries] of groups) {
    const isRejected = entries.some(e => e.isRecognized === false)
    const foreignBg = isRejected ? RED_ROW_BG : 'FFFFFF'
    const groupStart = row
    const groupEnd = row + entries.length - 1

    for (let i = 0; i < entries.length; i++) {
      const entry = entries[i]
      if (!entry) continue
      const slotBg = isRejected ? RED_ROW_BG : entry.courseSlotColor.replace('#', '')

      if (i === 0) {
        // Foreign course code: hyperlink if url available
        const codeUrl = (entry as any).foreignCourseUrl ?? ''
        ws[`A${row}`] = codeUrl
          ? link(entry.foreignCourseCode, codeUrl, foreignBg)
          : c(entry.foreignCourseCode, { bg: foreignBg, bold: true, halign: 'center' })

        // Foreign name EN: hyperlink if url available
        ws[`B${row}`] = codeUrl
          ? link(entry.foreignCourseNameEn, codeUrl, foreignBg)
          : c(entry.foreignCourseNameEn, { bg: foreignBg, wrap: true })

        ws[`C${row}`] = c(entry.enrollmentStatus, { bg: foreignBg })
        ws[`D${row}`] = c(entry.foreignCourseNameHr, { bg: foreignBg, wrap: true })
        ws[`E${row}`] = c(entry.foreignCourseHours, { bg: foreignBg, halign: 'center' })
        ws[`F${row}`] = c(entry.foreignCourseEcts, { bg: foreignBg, halign: 'center', bold: true })
      } else {
        for (const col of ['A', 'B', 'C', 'D', 'E', 'F']) ws[`${col}${row}`] = empty(foreignBg)
      }

      ws[`G${row}`] = c(i + 1, { bg: slotBg, halign: 'center' })
      ws[`H${row}`] = c(entry.courseSlotName, { bg: slotBg, wrap: true })
      ws[`I${row}`] = empty(slotBg)
      ws[`J${row}`] = c(entry.courseSlotCode, { bg: slotBg, halign: 'center' })
      ws[`K${row}`] = c(entry.courseSlotCategoryName, { bg: slotBg, wrap: true })
      ws[`L${row}`] = c(entry.courseSlotSemester, { bg: slotBg, halign: 'center' })
      ws[`M${row}`] = c(entry.awardedEcts, { bg: slotBg, halign: 'center', bold: true })

      if (i === 0) {
        ws[`N${row}`] = c(entry.originalGrade, { bg: foreignBg })
        ws[`O${row}`] = c(entry.ectsGrade, { bg: foreignBg, halign: 'center' })
        ws[`P${row}`] = c(entry.hrGrade, { bg: foreignBg, halign: 'center' })
        ws[`Q${row}`] = c(entry.examDate ?? '', { bg: foreignBg })
      } else {
        for (const col of ['N', 'O', 'P', 'Q']) ws[`${col}${row}`] = empty(foreignBg)
      }

      // Accumulate category totals
      const catKey = entry.courseSlotCategoryName
      if (!categoryTotals.has(catKey)) {
        categoryTotals.set(catKey, { name: catKey, color: entry.courseSlotColor.replace('#', ''), ects: 0 })
      }
      categoryTotals.get(catKey)!.ects = Math.round((categoryTotals.get(catKey)!.ects + entry.awardedEcts) * 10) / 10

      row++
    }

    // Merge foreign course columns (A–F, N–Q) across group rows if multi-slot
    if (entries.length > 1) {
      for (const ci of [0, 1, 2, 3, 4, 5, 13, 14, 15, 16]) {
        merges.push({ s: { r: groupStart - 1, c: ci }, e: { r: groupEnd - 1, c: ci } })
      }
    }
    // no spacer row, groups are contiguous, matching the screenshot
  }

  // UKUPNO row
  const totalEcts = Math.round(recognition.entries.reduce((s, e) => s + e.awardedEcts, 0) * 10) / 10
  ws[`A${row}`] = c(tr('ukupno', lang), { bold: true, bg: HEADER_BG, halign: 'right' })
  for (let ci = 1; ci <= 11; ci++) ws[`${colLetter(ci)}${row}`] = empty(HEADER_BG)
  merges.push({ s: { r: row - 1, c: 0 }, e: { r: row - 1, c: 11 } })
  ws[`M${row}`] = c(totalEcts, { bold: true, bg: HEADER_BG, halign: 'center' })
  for (const col of ['N', 'O', 'P', 'Q']) ws[`${col}${row}`] = empty(HEADER_BG)

  row += 2  // one blank spacer after UKUPNO

  // NAPOMENE section
  // Layout from screenshot:
  //   Col D = "NAPOMENE:" label (bold red), col E–M = napomena text (italic red)
  //   Col N–P = summary category rows (right side), col Q = blank no border

  function napomenaRow(r: number, label: string, text: string) {
    // A–C blank no border
    for (let ci = 0; ci <= 2; ci++) ws[`${colLetter(ci)}${r}`] = empty(undefined, false)
    // D = label
    ws[`D${r}`] = c(label, { sz: 8, bold: true, italic: true, color: 'FF0000', borders: false })
    // E–M = text
    ws[`E${r}`] = c(text, { sz: 8, italic: true, color: 'FF0000', borders: false, wrap: true })
    for (let ci = 5; ci <= 12; ci++) ws[`${colLetter(ci)}${r}`] = empty(undefined, false)
    merges.push({ s: { r: r - 1, c: 4 }, e: { r: r - 1, c: 12 } })
  }

  const napRow = row
  napomenaRow(row, tr('napomeneTitle', lang), tr('napomene1', lang)); row++
  napomenaRow(row, '', tr('napomene2', lang)); row++
  napomenaRow(row, '', tr('napomene3', lang))

  // Summary table (N-P columns, aligned with napomene rows)
  // Screenshot: N–O merged (category name), P (ECTS), Q blank no border
  let sumRow = napRow
  for (const [, cat] of categoryTotals) {
    ws[`N${sumRow}`] = c(cat.name, { bg: cat.color, sz: 8, borders: true, wrap: true })
    ws[`O${sumRow}`] = empty(cat.color, true)
    merges.push({ s: { r: sumRow - 1, c: 13 }, e: { r: sumRow - 1, c: 14 } })
    ws[`P${sumRow}`] = c(cat.ects, { bg: cat.color, halign: 'center', sz: 8, borders: true })
    ws[`Q${sumRow}`] = empty(undefined, false)
    sumRow++
  }
  // UKUPNO summary row
  ws[`N${sumRow}`] = c(tr('ukupno', lang), { bg: HEADER_BG, bold: true, sz: 8, borders: true })
  ws[`O${sumRow}`] = empty(HEADER_BG, true)
  merges.push({ s: { r: sumRow - 1, c: 13 }, e: { r: sumRow - 1, c: 14 } })
  ws[`P${sumRow}`] = c(totalEcts, { bg: HEADER_BG, bold: true, halign: 'center', sz: 8, borders: true })
  ws[`Q${sumRow}`] = empty(undefined, false)

  const lastRow = Math.max(row + 1, sumRow + 1)
  ws['!ref'] = `A1:Q${lastRow}`
  ws['!merges'] = merges

  ws['!cols'] = [
    { wch: 12 },  // A – šifra
    { wch: 38 },  // B – naziv EN
    { wch: 14 },  // C – status
    { wch: 36 },  // D – naziv HR
    { wch: 14 },  // E – sati
    { wch: 6 },   // F – ECTS
    { wch: 5 },   // G – Rbr
    { wch: 26 },  // H – slot naziv
    { wch: 4 },   // I – (merged with H)
    { wch: 10 },  // J – izb. grupa
    { wch: 22 },  // K – naziv izb. grupe
    { wch: 8 },   // L – semestar
    { wch: 10 },  // M – priznato ECTS
    { wch: 14 },  // N – ocjena orig / summary name
    { wch: 8 },   // O – ocjena ECTS / (merged)
    { wch: 8 },   // P – ocjena hrv / summary ECTS
    { wch: 12 },  // Q – datum
  ]

  // Row heights: fixed for header block, auto for data rows
  ws['!rows'] = [
    { hpt: 14 },  // 1 – title
    { hpt: 6 },   // 2 – spacer
    { hpt: 13 },  // 3
    { hpt: 13 },  // 4
    { hpt: 13 },  // 5
    { hpt: 13 },  // 6
    { hpt: 60 },  // 7 – big profile block starts
    { hpt: 13 },  // 8
    { hpt: 13 },  // 9
    { hpt: 13 },  // 10
    { hpt: 13 },  // 11
    { hpt: 13 },  // 12
    { hpt: 13 },  // 13 – section title
    { hpt: 6 },   // 14 – spacer
    { hpt: 6 },   // 15 – spacer
    { hpt: 40 },  // 16 – header row 1
    { hpt: 30 },  // 17 – header row 2
  ]

  return ws
}

// Sheet 2: Learning Agreement

function buildLASheet(
  la: LearningAgreementResponse,
  exchange: ExchangeResponse,
  lang: Lang,
): Record<string, XlsxCell> {
  const ws: XLSX.WorkSheet = {}
  const merges: XLSX.Range[] = []

  // Group flat entries by courseSlotId for quick lookup
  const stateMap = new Map<string, { mode: string; entries: LearningAgreementEntryResponse[] }>()
  for (const e of la.entries) {
    if (!stateMap.has(e.courseSlotId)) stateMap.set(e.courseSlotId, { mode: e.mode, entries: [] })
    if (e.foreignCourseId) stateMap.get(e.courseSlotId)!.entries.push(e)
  }

  const BORDER_COLOR = 'BFBFBF'
  const modeBorder = { style: 'thin' as const, color: { rgb: BORDER_COLOR } }

  // Total columns: col 0 = "Semesta", cols 1–30 = positions
  const TOTAL_COLS = 30

  // Row 1: study profile name (plain, bold, large)
  ws['A1'] = c(exchange.studyProfile.name, { bold: true, sz: 11, borders: false })
  for (let ci = 1; ci <= TOTAL_COLS; ci++) ws[`${colLetter(ci)}1`] = empty(undefined, false)
  merges.push({ s: { r: 0, c: 0 }, e: { r: 0, c: TOTAL_COLS } })

  // Row 2: blank spacer
  for (let ci = 0; ci <= TOTAL_COLS; ci++) ws[`${colLetter(ci)}2`] = empty(undefined, false)

  // Row 3: "Semesta" header + position numbers 1-30 + "Trans."
  ws['A3'] = c('Semesta', { bold: true, bg: HEADER_BG, halign: 'center', valign: 'middle' })
  for (let pos = 1; pos <= 30; pos++) {
    ws[`${colLetter(pos)}3`] = c(pos, { bold: true, bg: HEADER_BG, halign: 'center', sz: 8 })
  }

  // Rows 4-7: semesters 1-4
  for (let sem = 1; sem <= 4; sem++) {
    const rowNum = sem + 3  // sem1→row4, sem2→row5, sem3→row6, sem4→row7

    // Col A: semester number label
    ws[`A${rowNum}`] = c(sem, { bold: true, bg: HEADER_BG, halign: 'center', valign: 'middle' })

    // Fill all position cells with empty bordered cells as background
    for (let pos = 1; pos <= TOTAL_COLS; pos++) {
      ws[`${colLetter(pos)}${rowNum}`] = empty(undefined, true)
    }

    // Render each slot in this semester
    const semSlots = la.slots.filter(s => s.semester === sem)
    for (const slot of semSlots) {
      const state = stateMap.get(slot.id)
      const slotBg = slot.color.replace('#', '')
      // Build cell text: courseCode + newline + courseName + newline + mappings
      const lines: string[] = []
      if (slot.courseCode) lines.push(slot.courseCode)
      lines.push(slot.courseName)
      if (state?.entries.length) {
        for (const m of state.entries) {
          lines.push(`↳ ${m.foreignCourseCode} (${m.awardedEcts} ECTS)`)
        }
      }
      const text = lines.join('\n')

      const startCol = slot.slotPosition          // 1-indexed position column
      const endCol = slot.slotPosition + slot.ects - 1

      const cellStyle = {
        font: { name: FONT, sz: 7, bold: false, color: { rgb: '000000' } },
        fill: { fgColor: { rgb: slotBg } },
        alignment: { wrapText: true, horizontal: 'center' as const, vertical: 'middle' as const },
        border: { top: modeBorder, bottom: modeBorder, left: modeBorder, right: modeBorder },
      }

      ws[`${colLetter(startCol)}${rowNum}`] = { v: text, t: 's', s: cellStyle }

      for (let pos = startCol + 1; pos <= endCol; pos++) {
        ws[`${colLetter(pos)}${rowNum}`] = {
          v: '', t: 's',
          s: {
            fill: { fgColor: { rgb: slotBg } },
            border: { top: modeBorder, bottom: modeBorder, left: modeBorder, right: modeBorder },
          },
        }
      }

      if (endCol > startCol) {
        merges.push({ s: { r: rowNum - 1, c: startCol }, e: { r: rowNum - 1, c: endCol } })
      }
    }
  }

  // Legend: rows 9-11, col B (colour swatch) + col C-F (label text)
  // Placed below the 4-semester table, matching the screenshot style.
  // Col B = filled square (space with bg), col C–F merged = label text
  const LEGEND_ENTRIES = [
    { label: tr('laAtHome', lang),        swatchBg: '4472C4', textColor: '4472C4' },
    { label: tr('laAtExchange', lang),    swatchBg: 'FF0000', textColor: 'FF0000' },
    { label: tr('laAfterExchange', lang), swatchBg: '000000', textColor: '000000' },
  ]

  // Row 8: blank spacer between table and legend
  for (let ci = 0; ci <= TOTAL_COLS; ci++) ws[`${colLetter(ci)}8`] = empty(undefined, false)

  LEGEND_ENTRIES.forEach(({ label, swatchBg, textColor }, i) => {
    const r = 9 + i  // rows 9, 10, 11
    // Col A: blank
    ws[`A${r}`] = empty(undefined, false)
    // Col B: colour swatch cell
    ws[`B${r}`] = c('  ', { bg: swatchBg, borders: true })
    // Col C–F: label text, merged
    ws[`C${r}`] = c(label, { borders: false, sz: 9, color: textColor })
    for (let ci = 3; ci <= 6; ci++) ws[`${colLetter(ci)}${r}`] = empty(undefined, false)
    merges.push({ s: { r: r - 1, c: 2 }, e: { r: r - 1, c: 6 } })
  })

  ws['!ref'] = `A1:${colLetter(TOTAL_COLS)}11`
  ws['!merges'] = merges

  ws['!cols'] = [
    { wch: 7 },                       // A – "Semesta"
    ...Array(30).fill({ wch: 5.5 }), // positions 1–30
  ]

  ws['!rows'] = [
    { hpt: 20 },  // 1 – profile name
    { hpt: 6 },   // 2 – spacer
    { hpt: 20 },  // 3 – header
    { hpt: 60 },  // 4 – sem 1
    { hpt: 60 },  // 5 – sem 2
    { hpt: 60 },  // 6 – sem 3
    { hpt: 60 },  // 7 – sem 4
    { hpt: 6 },   // 8 – spacer
    { hpt: 14 },  // 9 – legend 1
    { hpt: 14 },  // 10 – legend 2
    { hpt: 14 },  // 11 – legend 3
  ]

  return ws
}

// Main export function

export function exportRecognitionExcel(
  recognition: RecognitionResponse,
  la: LearningAgreementResponse,
  exchange: ExchangeResponse,
  locale: string = 'hr',
): void {
  const lang: Lang = locale === 'en' ? 'en' : 'hr'
  const wb = XLSX.utils.book_new()

  const wsRecognition = buildRecognitionSheet(recognition, exchange, lang)
  XLSX.utils.book_append_sheet(wb, wsRecognition, tr('sheetRecognition', lang))

  const wsLA = buildLASheet(la, exchange, lang)
  XLSX.utils.book_append_sheet(wb, wsLA, tr('sheetLA', lang))

  const studentName = exchange.studentName.replace(/\s+/g, '_')
  const year = exchange.academicYear.replace('/', '-')
  XLSX.writeFile(wb, `Razmjena_${studentName}_${year}.xlsx`)
}