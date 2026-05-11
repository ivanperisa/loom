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

  // Helper: label in D (right-aligned), value in E (left-aligned) — no merges
  function infoRow(row: number, label: string, value: string | null, labelColor?: string) {
    ws[`D${row}`] = c(label, { bold: true, halign: 'right', borders: false, color: labelColor })
    ws[`E${row}`] = c(value ?? '', { halign: 'left', borders: false })
  }

  // Row 1: title — single cell, no merge
  ws['A1'] = c(tr('title', lang), { bold: true, sz: 11, borders: false })

  // Row 2: spacer (empty row kept)

  // Rows 3-6: student info
  infoRow(3, tr('student', lang), exchange.studentName)
  infoRow(4, tr('jmbag', lang), exchange.studentJmbag)
  infoRow(5, tr('studyType', lang), tr('studyTypeVal', lang))
  infoRow(6, tr('semester', lang), String(exchange.studySemester))

  // Row 8: profile name prominent, rows 7-12: exchange info
  ws['A8'] = c(`${tr('profileLabel', lang)} ${exchange.studyProfile.name}`, { bold: true, sz: 18, borders: false })
  const uniUrl = (exchange.foreignProgram as any).url ?? ''
  infoRow(7,  tr('university', lang),   uniUrl ? null : exchange.foreignProgram.institutionName, 'FF0000')
  if (uniUrl) ws[`E7`] = link(exchange.foreignProgram.institutionName, uniUrl)
  infoRow(9,  tr('faculty', lang),      exchange.foreignProgram.name)
  infoRow(10, tr('academicYear', lang),  exchange.academicYear)
  infoRow(11, tr('exchSemester', lang),  exchange.semesterType === exchangeSemester.Winter ? tr('winter', lang) : tr('summer', lang))
  infoRow(12, tr('mentor', lang),        exchange.mentor)

  // Row 14: section subtitle — single cell, no merge
  ws['A14'] = c(tr('sectionTitle', lang), { sz: 9, italic: true, color: 'FF0000', borders: false })

  // Row 13, 15: spacers (empty rows kept)

  // Row 16: single header row
  const hdr      = (v: string) => c(v, { bold: true, bg: 'FFFFCC', wrap: true, halign: 'center', valign: 'middle' })
  const hdrGrade = (v: string) => c(v, { bold: true, bg: 'DDD9C3', wrap: true, halign: 'center', valign: 'middle' })

  ws['A16'] = hdr(tr('colForeignCode', lang))
  ws['B16'] = hdr(tr('colNameEn', lang))
  ws['C16'] = hdr(tr('colStatus', lang))
  ws['D16'] = hdr(tr('colNameHr', lang))
  ws['E16'] = hdr(tr('colHours', lang))
  ws['F16'] = hdr(tr('colEcts', lang))
  ws['G16'] = hdr(tr('colRbr', lang))
  ws['H16'] = hdr(tr('colRecognizedAs', lang))
  ws['I16'] = hdr(tr('colSlotName', lang))
  ws['J16'] = hdr(tr('colSlotCode', lang))
  ws['K16'] = hdr(tr('colSlotCategory', lang))
  ws['L16'] = hdr(tr('colSemester', lang))
  ws['M16'] = hdr(tr('colAwarded', lang))
  ws['N16'] = hdrGrade(tr('colOrigGrade', lang))
  ws['O16'] = hdrGrade(tr('colEctsGrade', lang))
  ws['P16'] = hdrGrade(tr('colHrGrade', lang))
  ws['Q16'] = hdrGrade(tr('colDate', lang))

  // Data rows (from row 17), no spacer rows between groups
  const groups = new Map<string, typeof recognition.entries>()
  for (const entry of recognition.entries) {
    if (!groups.has(entry.foreignCourseCode)) groups.set(entry.foreignCourseCode, [])
    groups.get(entry.foreignCourseCode)!.push(entry)
  }

  let row = 17
  const categoryTotals = new Map<string, { name: string; color: string; ects: number }>()

  for (const [, entries] of groups) {
    const isRejected = entries.some(e => e.isRecognized === false)
    const foreignBg = isRejected ? RED_ROW_BG : 'FFFFFF'
    const gradeBg = isRejected ? RED_ROW_BG : 'DDD9C3'
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

      ws[`G${row}`] = c(i + 1, { halign: 'center' })
      ws[`H${row}`] = c(entry.courseSlotCode, { halign: 'center' })
      ws[`I${row}`] = c(entry.courseSlotName, { wrap: true })
      ws[`J${row}`] = c(entry.courseSlotCategoryCode, { halign: 'center' })
      ws[`K${row}`] = c(entry.courseSlotCategoryName, { bg: slotBg, wrap: true })
      ws[`L${row}`] = c(entry.courseSlotSemester, { halign: 'center' })
      ws[`M${row}`] = c(entry.awardedEcts, { bg: slotBg, halign: 'center', bold: true })

      if (i === 0) {
        ws[`N${row}`] = c(entry.originalGrade, { bg: gradeBg })
        ws[`O${row}`] = c(entry.ectsGrade, { bg: gradeBg, halign: 'center' })
        ws[`P${row}`] = c(entry.hrGrade, { bg: gradeBg, halign: 'center' })
        ws[`Q${row}`] = c(entry.examDate ?? '', { bg: gradeBg })
      } else {
        for (const col of ['N', 'O', 'P', 'Q']) ws[`${col}${row}`] = empty(gradeBg)
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

  // NAPOMENE section — B=label (right), C=text (left), no merges
  const napRow = row
  ws[`B${row}`] = c(tr('napomeneTitle', lang), { sz: 8, bold: true, italic: true, color: 'FF0000', halign: 'right', borders: false })
  ws[`C${row}`] = c(tr('napomene1', lang), { sz: 8, italic: true, color: 'FF0000', halign: 'left', borders: false })
  row++
  ws[`C${row}`] = c(tr('napomene2', lang), { sz: 8, italic: true, color: 'FF0000', halign: 'left', borders: false })
  row++
  ws[`C${row}`] = c(tr('napomene3', lang), { sz: 8, italic: true, color: 'FF0000', halign: 'left', borders: false })

  // Summary table (N–O columns, aligned with napomene rows) — no merges
  let sumRow = napRow
  for (const [, cat] of categoryTotals) {
    ws[`N${sumRow}`] = c(cat.name, { bg: cat.color, sz: 8, borders: true, wrap: true })
    ws[`O${sumRow}`] = c(cat.ects, { bg: cat.color, halign: 'center', sz: 8, borders: true })
    sumRow++
  }
  // UKUPNO summary row
  ws[`N${sumRow}`] = c(tr('ukupno', lang), { bg: HEADER_BG, bold: true, sz: 8, borders: true })
  ws[`O${sumRow}`] = c(totalEcts, { bg: HEADER_BG, bold: true, halign: 'center', sz: 8, borders: true })

  const lastRow = Math.max(row + 1, sumRow + 1)
  ws['!ref'] = `A1:Q${lastRow}`
  ws['!merges'] = merges

  ws['!cols'] = [
    { wch: 16 },  // A – šifra
    { wch: 49 },  // B – naziv EN
    { wch: 16 },  // C – status
    { wch: 59 },  // D – naziv HR
    { wch: 25 },  // E – sati
    { wch: 6 },   // F – ECTS
    { wch: 5 },   // G – Rbr
    { wch: 12 },  // H – kod predmeta (courseSlotCode)
    { wch: 26 },  // I – naziv predmeta (courseSlotName)
    { wch: 10 },  // J – izb. grupa kod (courseSlotCategoryCode)
    { wch: 22 },  // K – naziv izb. grupe
    { wch: 8 },   // L – semestar
    { wch: 10 },  // M – priznato ECTS
    { wch: 14 },  // N – ocjena orig / summary name
    { wch: 8 },   // O – ocjena ECTS / (merged)
    { wch: 8 },   // P – ocjena hrv / summary ECTS
    { wch: 14 },  // Q – datum
  ]

  ws['!rows'] = [
    { hpt: 14 },  // 1 – title
    { hpt: 15 },  // 2 – spacer
    { hpt: 13 },  // 3
    { hpt: 13 },  // 4
    { hpt: 13 },  // 5
    { hpt: 13 },  // 6
    { hpt: 13 },  // 7
    { hpt: 24 },  // 8 – profile name (font 18)
    { hpt: 13 },  // 9
    { hpt: 13 },  // 10
    { hpt: 13 },  // 11
    { hpt: 13 },  // 12
    { hpt: 15 },  // 13 – spacer
    { hpt: 13 },  // 14 – section title
    { hpt: 15 },  // 15 – spacer
    { hpt: 40 },  // 16 – header row
  ]

  return ws
}

// Sheet 2: Learning Agreement

const MODE_TEXT_COLOR: Record<string, string> = {
  AtHome: '4472C4',
  AtExchange: 'FF0000',
  AfterExchange: '000000',
}

function buildLASheet(
  la: LearningAgreementResponse,
  exchange: ExchangeResponse,
  lang: Lang,
): Record<string, XlsxCell> {
  const ws: XLSX.WorkSheet = {}
  const merges: XLSX.Range[] = []

  // Group flat entries by courseSlotId for quick lookup; skip deleted entries
  const stateMap = new Map<string, { mode: string; entries: LearningAgreementEntryResponse[] }>()
  for (const e of la.entries) {
    if (!stateMap.has(e.courseSlotId)) stateMap.set(e.courseSlotId, { mode: e.mode, entries: [] })
    if (e.foreignCourseId && !e.isDeleted) stateMap.get(e.courseSlotId)!.entries.push(e)
  }

  // Total columns: col 0 = "Semestar", cols 1–30 = positions
  const TOTAL_COLS = 30

  // Row 1: study profile name (plain, bold, large)
  ws['A1'] = c(exchange.studyProfile.name, { bold: true, sz: 11, borders: false })
  for (let ci = 1; ci <= TOTAL_COLS; ci++) ws[`${colLetter(ci)}1`] = empty(undefined, false)
  merges.push({ s: { r: 0, c: 0 }, e: { r: 0, c: TOTAL_COLS } })

  // Row 2: blank spacer
  for (let ci = 0; ci <= TOTAL_COLS; ci++) ws[`${colLetter(ci)}2`] = empty(undefined, false)

  // Row 3: "Semestar" header + position numbers 1-30 + "Trans."
  ws['A3'] = c('Semestar', { bold: true, bg: HEADER_BG, halign: 'center', valign: 'middle' })
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

      // Build cell text matching LearningAgreementPanel layout
      const lines: string[] = []
      if (slot.courseCode) lines.push(slot.courseCode)
      lines.push(slot.courseName)
      for (const m of state?.entries ?? []) {
        lines.push(m.foreignCourseCode ?? '')
        if (m.foreignCourseNameEn) lines.push(`  ${m.foreignCourseNameEn}`)
        if (m.foreignCourseNameHr) lines.push(`  ${m.foreignCourseNameHr}`)
        lines.push(`  ${m.awardedEcts} ECTS`)
      }
      const text = lines.join('\n')

      const startCol = slot.slotPosition          // 1-indexed position column
      const endCol = slot.slotPosition + slot.ects - 1

      const textColor = state?.mode ? (MODE_TEXT_COLOR[state.mode] ?? '000000') : '000000'
      const slotBorder = { style: 'thin' as const, color: { rgb: 'BFBFBF' } }
      const slotBorderAll = { top: slotBorder, bottom: slotBorder, left: slotBorder, right: slotBorder }
      const cellStyle = {
        font: { name: FONT, sz: 11, bold: false, color: { rgb: textColor } },
        fill: { fgColor: { rgb: slotBg } },
        alignment: { wrapText: true, horizontal: 'center' as const, vertical: 'middle' as const },
        border: slotBorderAll,
      }

      ws[`${colLetter(startCol)}${rowNum}`] = { v: text, t: 's', s: cellStyle }

      for (let pos = startCol + 1; pos <= endCol; pos++) {
        ws[`${colLetter(pos)}${rowNum}`] = {
          v: '', t: 's',
          s: { fill: { fgColor: { rgb: slotBg } }, border: slotBorderAll, alignment: { vertical: 'middle' as const } },
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
    { wch: 7 },                       // A – "Semestar"
    ...Array(30).fill({ wch: 5.5 }), // positions 1–30
  ]

  ws['!rows'] = [
    { hpt: 20 },  // 1 – profile name
    { hpt: 6 },   // 2 – spacer
    { hpt: 20 },  // 3 – header
    { hpt: 100 },  // 4 – sem 1
    { hpt: 100 },  // 5 – sem 2
    { hpt: 100 },  // 6 – sem 3
    { hpt: 100 },  // 7 – sem 4
    { hpt: 6 },   // 8 – spacer
    { hpt: 14 },  // 9 – legend 1
    { hpt: 14 },  // 10 – legend 2
    { hpt: 14 },  // 11 – legend 3
  ]

  return ws
}

// Main export function

export function exportExchangeExcel(
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