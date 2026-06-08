import XLSX from 'xlsx-js-style'
import { exchangeSemester } from '@/utils/exchangeSemester'
import type { RecognitionResponse } from '@/types/recognition.types'
import type { ExchangeResponse } from '@/types/exchange.types'
import type { LearningAgreementResponse, LearningAgreementEntryResponse } from '@/types/learningAgreement.types'

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
  napomene3:        { hr: 'Poveznice zamijeniti stvarnim poveznicama na strane/domaće kolegije', en: 'Replace links with actual links to partner/domestic courses' },
  sheetRecognition: { hr: 'Priznavanje', en: 'Recognition' },
  sheetLA:          { hr: 'Ugovor o učenju', en: 'Learning Agreement' },
  colPartnerCode:   { hr: 'Šifra predmeta', en: 'Course Code' },
  colName:          { hr: 'Naziv (engleski)', en: 'Name (English)' },
  colStatus:        { hr: 'Status predmeta', en: 'Course Status' },
  colNameHr:        { hr: 'Naziv (hrvatski)', en: 'Name (Croatian)' },
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

  function infoRow(row: number, label: string, value: string | null, labelColor?: string) {
    ws[`D${row}`] = c(label, { bold: true, halign: 'right', borders: false, color: labelColor })
    ws[`E${row}`] = c(value ?? '', { halign: 'left', borders: false })
  }

  ws['A1'] = c(tr('title', lang), { bold: true, sz: 11, borders: false })

  infoRow(3, tr('student', lang), exchange.studentName)
  infoRow(4, tr('jmbag', lang), exchange.studentJmbag)
  infoRow(5, tr('studyType', lang), tr('studyTypeVal', lang))
  infoRow(6, tr('semester', lang), exchange.studySemesters.slice().sort((a, b) => a - b).join(', '))

  ws['A8'] = c(`${tr('profileLabel', lang)} ${exchange.homeProfile.name}`, { bold: true, sz: 18, borders: false })
  infoRow(7,  tr('university', lang),   exchange.partnerInstitutionName, 'FF0000')
  infoRow(9,  tr('faculty', lang),      '')
  infoRow(10, tr('academicYear', lang),  exchange.academicYear)
  infoRow(11, tr('exchSemester', lang),  exchange.semesterType === exchangeSemester.Winter ? tr('winter', lang) : tr('summer', lang))
  infoRow(12, tr('mentor', lang),        exchange.mentor)

  ws['A14'] = c(tr('sectionTitle', lang), { sz: 9, italic: true, color: 'FF0000', borders: false })

  const hdr      = (v: string) => c(v, { bold: true, bg: 'FFFFCC', wrap: true, halign: 'center', valign: 'middle' })
  const hdrGrade = (v: string) => c(v, { bold: true, bg: 'DDD9C3', wrap: true, halign: 'center', valign: 'middle' })

  ws['A16'] = hdr(tr('colPartnerCode', lang))
  ws['B16'] = hdr(tr('colName', lang))
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

  const groups = new Map<string, typeof recognition.entries>()
  for (const entry of recognition.entries) {
    if (!groups.has(entry.partnerCourseCode)) groups.set(entry.partnerCourseCode, [])
    groups.get(entry.partnerCourseCode)!.push(entry)
  }

  let row = 17
  const categoryTotals = new Map<string, { name: string; color: string; ects: number }>()

  for (const [, entries] of groups) {
    const isRejected = entries.some(e => e.isRecognized === false)
    const partnerBg = isRejected ? RED_ROW_BG : 'FFFFFF'
    const gradeBg = isRejected ? RED_ROW_BG : 'DDD9C3'
    const groupStart = row
    const groupEnd = row + entries.length - 1

    for (let i = 0; i < entries.length; i++) {
      const entry = entries[i]
      if (!entry) continue
      const slotBg = isRejected ? RED_ROW_BG : entry.homeSlotColor.replace('#', '')

      if (i === 0) {
        const codeUrl = (entry as any).partnerCourseUrl ?? ''
        ws[`A${row}`] = codeUrl
          ? link(entry.partnerCourseCode, codeUrl, partnerBg)
          : c(entry.partnerCourseCode, { bg: partnerBg, bold: true, halign: 'center' })

        ws[`B${row}`] = codeUrl
          ? link(entry.partnerCourseName, codeUrl, partnerBg)
          : c(entry.partnerCourseName, { bg: partnerBg, wrap: true })

        ws[`C${row}`] = c(entry.enrollmentStatus, { bg: partnerBg })
        ws[`D${row}`] = c(entry.partnerCourseNameHr, { bg: partnerBg, wrap: true })
        ws[`E${row}`] = c(entry.partnerCourseHours, { bg: partnerBg, halign: 'center' })
        ws[`F${row}`] = c(entry.partnerCourseEcts, { bg: partnerBg, halign: 'center', bold: true })
      } else {
        for (const col of ['A', 'B', 'C', 'D', 'E', 'F']) ws[`${col}${row}`] = empty(partnerBg)
      }

      ws[`G${row}`] = c(i + 1, { halign: 'center' })
      ws[`H${row}`] = c(entry.homeSlotCourseIsvuCode, { halign: 'center' })
      ws[`I${row}`] = c(entry.homeSlotCourseName, { wrap: true })
      ws[`J${row}`] = c(entry.homeSlotCourseGroupIsvuCode, { halign: 'center' })
      ws[`K${row}`] = c(entry.homeSlotCourseGroupName, { bg: slotBg, wrap: true })
      ws[`L${row}`] = c(entry.homeSlotSemester, { halign: 'center' })
      ws[`M${row}`] = c(entry.awardedEcts, { bg: slotBg, halign: 'center', bold: true })

      if (i === 0) {
        ws[`N${row}`] = c(entry.originalGrade, { bg: gradeBg })
        ws[`O${row}`] = c(entry.ectsGrade, { bg: gradeBg, halign: 'center' })
        ws[`P${row}`] = c(entry.hrGrade, { bg: gradeBg, halign: 'center' })
        ws[`Q${row}`] = c(entry.examDate ?? '', { bg: gradeBg })
      } else {
        for (const col of ['N', 'O', 'P', 'Q']) ws[`${col}${row}`] = empty(gradeBg)
      }

      const catKey = entry.homeSlotCourseGroupName
      if (!categoryTotals.has(catKey)) {
        categoryTotals.set(catKey, { name: catKey, color: entry.homeSlotColor.replace('#', ''), ects: 0 })
      }
      categoryTotals.get(catKey)!.ects = Math.round((categoryTotals.get(catKey)!.ects + entry.awardedEcts) * 10) / 10

      row++
    }

    if (entries.length > 1) {
      for (const ci of [0, 1, 2, 3, 4, 5, 13, 14, 15, 16]) {
        merges.push({ s: { r: groupStart - 1, c: ci }, e: { r: groupEnd - 1, c: ci } })
      }
    }
  }

  const totalEcts = Math.round(recognition.entries.reduce((s, e) => s + e.awardedEcts, 0) * 10) / 10
  ws[`A${row}`] = c(tr('ukupno', lang), { bold: true, bg: HEADER_BG, halign: 'right' })
  for (let ci = 1; ci <= 11; ci++) ws[`${colLetter(ci)}${row}`] = empty(HEADER_BG)
  merges.push({ s: { r: row - 1, c: 0 }, e: { r: row - 1, c: 11 } })
  ws[`M${row}`] = c(totalEcts, { bold: true, bg: HEADER_BG, halign: 'center' })
  for (const col of ['N', 'O', 'P', 'Q']) ws[`${col}${row}`] = empty(HEADER_BG)

  row += 2

  const napRow = row
  ws[`B${row}`] = c(tr('napomeneTitle', lang), { sz: 8, bold: true, italic: true, color: 'FF0000', halign: 'right', borders: false })
  ws[`C${row}`] = c(tr('napomene1', lang), { sz: 8, italic: true, color: 'FF0000', halign: 'left', borders: false })
  row++
  ws[`C${row}`] = c(tr('napomene2', lang), { sz: 8, italic: true, color: 'FF0000', halign: 'left', borders: false })
  row++
  ws[`C${row}`] = c(tr('napomene3', lang), { sz: 8, italic: true, color: 'FF0000', halign: 'left', borders: false })

  let sumRow = napRow
  for (const [, cat] of categoryTotals) {
    ws[`N${sumRow}`] = c(cat.name, { bg: cat.color, sz: 8, borders: true, wrap: true })
    ws[`O${sumRow}`] = c(cat.ects, { bg: cat.color, halign: 'center', sz: 8, borders: true })
    sumRow++
  }
  ws[`N${sumRow}`] = c(tr('ukupno', lang), { bg: HEADER_BG, bold: true, sz: 8, borders: true })
  ws[`O${sumRow}`] = c(totalEcts, { bg: HEADER_BG, bold: true, halign: 'center', sz: 8, borders: true })

  const lastRow = Math.max(row + 1, sumRow + 1)
  ws['!ref'] = `A1:Q${lastRow}`
  ws['!merges'] = merges

  ws['!cols'] = [
    { wch: 16 },
    { wch: 49 },
    { wch: 16 },
    { wch: 59 },
    { wch: 25 },
    { wch: 6 },
    { wch: 5 },
    { wch: 12 },
    { wch: 26 },
    { wch: 10 },
    { wch: 22 },
    { wch: 8 },
    { wch: 10 },
    { wch: 14 },
    { wch: 8 },
    { wch: 8 },
    { wch: 14 },
  ]

  ws['!rows'] = [
    { hpt: 14 },
    { hpt: 15 },
    { hpt: 13 },
    { hpt: 13 },
    { hpt: 13 },
    { hpt: 13 },
    { hpt: 13 },
    { hpt: 24 },
    { hpt: 13 },
    { hpt: 13 },
    { hpt: 13 },
    { hpt: 13 },
    { hpt: 15 },
    { hpt: 13 },
    { hpt: 15 },
    { hpt: 40 },
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

  const stateMap = new Map<string, { mode: string; entries: LearningAgreementEntryResponse[] }>()
  for (const e of la.entries) {
    if (!stateMap.has(e.homeSlotId)) stateMap.set(e.homeSlotId, { mode: e.mode, entries: [] })
    if (e.partnerCourseId && !e.isDeleted) stateMap.get(e.homeSlotId)!.entries.push(e)
  }

  const TOTAL_COLS = 30

  ws['A1'] = c(exchange.homeProfile.name, { bold: true, sz: 11, borders: false })
  for (let ci = 1; ci <= TOTAL_COLS; ci++) ws[`${colLetter(ci)}1`] = empty(undefined, false)
  merges.push({ s: { r: 0, c: 0 }, e: { r: 0, c: TOTAL_COLS } })

  for (let ci = 0; ci <= TOTAL_COLS; ci++) ws[`${colLetter(ci)}2`] = empty(undefined, false)

  ws['A3'] = c('Semestar', { bold: true, bg: HEADER_BG, halign: 'center', valign: 'middle' })
  for (let pos = 1; pos <= 30; pos++) {
    ws[`${colLetter(pos)}3`] = c(pos, { bold: true, bg: HEADER_BG, halign: 'center', sz: 8 })
  }

  for (let sem = 1; sem <= 4; sem++) {
    const rowNum = sem + 3

    ws[`A${rowNum}`] = c(sem, { bold: true, bg: HEADER_BG, halign: 'center', valign: 'middle' })

    for (let pos = 1; pos <= TOTAL_COLS; pos++) {
      ws[`${colLetter(pos)}${rowNum}`] = empty(undefined, true)
    }

    const semSlots = la.slots.filter(s => s.semester === sem)
    for (const slot of semSlots) {
      const state = stateMap.get(slot.id)
      const slotBg = slot.color.replace('#', '')

      const isvuCode = slot.courseIsvuCode ?? slot.courseGroupIsvuCode
      const name = slot.courseName ?? slot.courseGroupName ?? ''

      const lines: string[] = []
      if (isvuCode) lines.push(String(isvuCode))
      lines.push(name)
      for (const m of state?.entries ?? []) {
        lines.push(m.partnerCourseCode ?? '')
        if (m.partnerCourseName) lines.push(`  ${m.partnerCourseName}`)
        if (m.partnerCourseNameHr) lines.push(`  ${m.partnerCourseNameHr}`)
        lines.push(`  ${m.awardedEcts} ECTS`)
      }
      const text = lines.join('\n')

      const startCol = slot.slotPosition
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

  const LEGEND_ENTRIES = [
    { label: tr('laAtHome', lang),        swatchBg: '4472C4', textColor: '4472C4' },
    { label: tr('laAtExchange', lang),    swatchBg: 'FF0000', textColor: 'FF0000' },
    { label: tr('laAfterExchange', lang), swatchBg: '000000', textColor: '000000' },
  ]

  for (let ci = 0; ci <= TOTAL_COLS; ci++) ws[`${colLetter(ci)}8`] = empty(undefined, false)

  LEGEND_ENTRIES.forEach(({ label, swatchBg, textColor }, i) => {
    const r = 9 + i
    ws[`A${r}`] = empty(undefined, false)
    ws[`B${r}`] = c('  ', { bg: swatchBg, borders: true })
    ws[`C${r}`] = c(label, { borders: false, sz: 9, color: textColor })
    for (let ci = 3; ci <= 6; ci++) ws[`${colLetter(ci)}${r}`] = empty(undefined, false)
    merges.push({ s: { r: r - 1, c: 2 }, e: { r: r - 1, c: 6 } })
  })

  ws['!ref'] = `A1:${colLetter(TOTAL_COLS)}11`
  ws['!merges'] = merges

  ws['!cols'] = [
    { wch: 7 },
    ...Array(30).fill({ wch: 5.5 }),
  ]

  ws['!rows'] = [
    { hpt: 20 },
    { hpt: 6 },
    { hpt: 20 },
    { hpt: 100 },
    { hpt: 100 },
    { hpt: 100 },
    { hpt: 100 },
    { hpt: 6 },
    { hpt: 14 },
    { hpt: 14 },
    { hpt: 14 },
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
