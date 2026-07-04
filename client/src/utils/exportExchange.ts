import XLSX from 'xlsx-js-style'
import { exchangeSemester } from '@/utils/exchangeSemester'
import type { RecognitionResponse, RecognitionEntryResponse } from '@/types/recognition.types'
import type { ExchangeResponse } from '@/types/exchange.types'
import type { LearningAgreementResponse } from '@/types/learningAgreement.types'
import type { MappingSchemeResponse, MappingSchemeEntryResponse } from '@/types/mappingScheme.types'

type ExportEntry = RecognitionEntryResponse | MappingSchemeEntryResponse

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

function empty(bg?: string, borders = true): XlsxCell { return c('', { bg, borders }) }

function formatDdMmYyyy(iso: string | null | undefined): string {
  if (!iso) return ''
  const [y, m, d] = iso.split('-')
  return y && m && d ? `${d}/${m}/${y}` : iso
}

function colLetter(idx: number): string {
  let s = ''; let n = idx + 1
  while (n > 0) { const r = (n - 1) % 26; s = String.fromCharCode(65 + r) + s; n = Math.floor((n - 1) / 26) }
  return s
}

// Translation map

type Lang = 'hr' | 'en' | string

const T: Record<string, Record<Lang, string>> = {
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
  mandatoryCourse:  { hr: 'Obavezan predmet', en: 'Mandatory course' },
  colSemester:      { hr: 'Semestar', en: 'Semester' },
  colAwarded:       { hr: 'Priznato ECTS-a', en: 'Awarded ECTS' },
  colOrigGrade:     { hr: 'Ocjena\noriginalna', en: 'Original\nGrade' },
  colEctsGrade:     { hr: 'Ocjena\nECTS\n(F-A)', en: 'ECTS\nGrade\n(F-A)' },
  colHrGrade:       { hr: 'Ocjena\nhrv.\n(1-5)', en: 'Croatian\nGrade\n(1-5)' },
  colDate:          { hr: 'Datum polaganja', en: 'Exam Date' },
  statusPassed:     { hr: 'Položeno', en: 'Passed' },
  statusNotPassed:  { hr: 'Nepoloženo', en: 'Not passed' },
  laAtHome:         { hr: 'Položeno na FER-u', en: 'Taken at home institution' },
}

function tr(key: string, lang: Lang): string {
  return T[key]?.[lang] ?? T[key]?.['hr'] ?? key
}

// Sheet 1: Recognition

function buildRecognitionSheet(
  entries: ExportEntry[],
  exchange: ExchangeResponse,
  lang: Lang,
): Record<string, XlsxCell> {
  const ws: XLSX.WorkSheet = {}
  const merges: XLSX.Range[] = []

  function infoRow(row: number, label: string, value: string | null, labelColor?: string) {
    ws[`D${row}`] = c(label, { bold: true, halign: 'right', borders: false, color: labelColor })
    ws[`E${row}`] = c(value ?? '', { halign: 'left', borders: false })
  }

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

  const groups = new Map<string, ExportEntry[]>()
  for (const entry of entries) {
    if (!groups.has(entry.partnerCourseCode)) groups.set(entry.partnerCourseCode, [])
    groups.get(entry.partnerCourseCode)!.push(entry)
  }

  let row = 17
  const categoryTotals = new Map<string, { name: string; color: string; ects: number }>()

  function buildDisplayRows(entries: ExportEntry[]): ExportEntry[][] {
    const rows: ExportEntry[][] = []
    const groupRowIndex = new Map<number, number>()
    for (const entry of entries) {
      if (entry.homeSlotCourseIsvuCode == null && entry.homeSlotCourseGroupIsvuCode != null) {
        const existing = groupRowIndex.get(entry.homeSlotCourseGroupIsvuCode)
        if (existing !== undefined) {
          rows[existing]!.push(entry)
          continue
        }
        groupRowIndex.set(entry.homeSlotCourseGroupIsvuCode, rows.length)
      }
      rows.push([entry])
    }
    return rows
  }

  for (const [, entries] of groups) {
    const isNotPassed = entries.some(e => e.enrollmentStatus === 'NotPassed')
    const partnerBg = isNotPassed ? RED_ROW_BG : 'FFFFFF'
    const gradeBg = isNotPassed ? RED_ROW_BG : 'DDD9C3'
    const displayRows = buildDisplayRows(entries)
    const groupStart = row
    const groupEnd = row + displayRows.length - 1

    for (let i = 0; i < displayRows.length; i++) {
      const merged = displayRows[i]
      if (!merged) continue
      const entry = merged[0]
      if (!entry) continue
      const mergedEcts = Math.round(merged.reduce((s, e) => s + e.awardedEcts, 0) * 10) / 10
      const slotBg = isNotPassed ? RED_ROW_BG : entry.homeSlotColor.replace('#', '')
      const plainBg = isNotPassed ? RED_ROW_BG : undefined

      if (i === 0) {
        ws[`A${row}`] = c(entry.partnerCourseCode, { bg: partnerBg, bold: true, halign: 'center' })
        ws[`B${row}`] = c(entry.partnerCourseName, { bg: partnerBg, wrap: true })

        const statusKey = entry.enrollmentStatus === 'Passed' ? 'statusPassed'
          : entry.enrollmentStatus === 'NotPassed' ? 'statusNotPassed' : null
        ws[`C${row}`] = c(statusKey ? tr(statusKey, lang) : '', { bg: partnerBg })
        ws[`D${row}`] = c(entry.partnerCourseNameHr, { bg: partnerBg, wrap: true })
        ws[`E${row}`] = c(entry.partnerCourseHours, { bg: partnerBg, halign: 'center' })
        ws[`F${row}`] = c(entry.partnerCourseEcts, { bg: partnerBg, halign: 'center', bold: true })
      } else {
        for (const col of ['A', 'B', 'C', 'D', 'E', 'F']) ws[`${col}${row}`] = empty(partnerBg)
      }

      ws[`G${row}`] = c(i + 1, { bg: plainBg, halign: 'center' })
      ws[`H${row}`] = c(entry.homeSlotCourseIsvuCode, { bg: plainBg, halign: 'center' })
      ws[`I${row}`] = c(entry.homeSlotCourseName, { bg: plainBg, wrap: true })
      ws[`J${row}`] = c(entry.homeSlotCourseGroupIsvuCode, { bg: plainBg, halign: 'center' })
      ws[`K${row}`] = c(entry.homeSlotCourseGroupName || tr('mandatoryCourse', lang), { bg: slotBg, wrap: true })
      ws[`L${row}`] = c(entry.homeSlotSemester, { bg: plainBg, halign: 'center' })
      ws[`M${row}`] = c(mergedEcts, { bg: slotBg, halign: 'center', bold: true })

      if (i === 0) {
        ws[`N${row}`] = c(entry.originalGrade, { bg: gradeBg, halign: 'center' })
        ws[`O${row}`] = c(entry.ectsGrade, { bg: gradeBg, halign: 'center' })
        ws[`P${row}`] = c(entry.hrGrade, { bg: gradeBg, halign: 'center' })
        ws[`Q${row}`] = c(formatDdMmYyyy(entry.examDate), { bg: gradeBg })
      } else {
        for (const col of ['N', 'O', 'P', 'Q']) ws[`${col}${row}`] = empty(gradeBg)
      }

      if (!isNotPassed) {
        const catKey = entry.homeSlotCourseGroupName || tr('mandatoryCourse', lang)
        if (!categoryTotals.has(catKey)) {
          categoryTotals.set(catKey, { name: catKey, color: entry.homeSlotColor.replace('#', ''), ects: 0 })
        }
        categoryTotals.get(catKey)!.ects = Math.round((categoryTotals.get(catKey)!.ects + mergedEcts) * 10) / 10
      }

      row++
    }

    if (displayRows.length > 1) {
      for (const ci of [0, 1, 2, 3, 4, 5, 13, 14, 15, 16]) {
        merges.push({ s: { r: groupStart - 1, c: ci }, e: { r: groupEnd - 1, c: ci } })
      }
    }
  }

  const totalEcts = Math.round(
    entries.filter(e => e.enrollmentStatus !== 'NotPassed').reduce((s, e) => s + e.awardedEcts, 0) * 10,
  ) / 10
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

const MODE_OUTLINE_COLOR: Record<string, string> = {
  AtHome: '4472C4',
}

interface LaEntryLine {
  code: string
  name: string
  nameHr: string | null
  ects: number
  deleted: boolean
}

const CHARS_PER_COL = 6.5
const LINE_HEIGHT_PT = 11.5
const DEFAULT_ENTRY_ROW_PT = 34

function wrappedLineCount(text: string, colWidthChars: number): number {
  if (!text) return 1
  return Math.max(1, Math.ceil(text.length / colWidthChars))
}

function entryRowHeightPt(entry: LaEntryLine, slotEcts: number): number {
  const colWidthChars = Math.max(1, slotEcts) * CHARS_PER_COL
  let lines = wrappedLineCount(entry.name, colWidthChars)
  if (entry.nameHr) lines += wrappedLineCount(entry.nameHr, colWidthChars)
  lines += 2
  return Math.max(DEFAULT_ENTRY_ROW_PT, lines * LINE_HEIGHT_PT)
}

function buildLASheet(
  la: LearningAgreementResponse,
  mappingEntries: MappingSchemeEntryResponse[],
  exchange: ExchangeResponse,
  lang: Lang,
): Record<string, XlsxCell> {
  const ws: XLSX.WorkSheet = {}
  const merges: XLSX.Range[] = []

  type SlotState = { mode: string; entries: { partnerCourseCode: string | null; partnerCourseName: string | null; partnerCourseNameHr: string | null; awardedEcts: number | null; enrollmentStatus?: string | null }[] }
  const stateMap = new Map<string, SlotState>()

  if (mappingEntries.length > 0) {
    const laModeBySlot = new Map<string, string>()
    for (const e of la.entries) {
      if (!e.isDeleted) laModeBySlot.set(e.homeSlotId, e.mode)
    }
    for (const e of mappingEntries) {
      if (!stateMap.has(e.homeSlotId)) stateMap.set(e.homeSlotId, { mode: 'AtExchange', entries: [] })
      stateMap.get(e.homeSlotId)!.entries.push(e)
    }
    for (const [slotId, mode] of laModeBySlot) {
      if (!stateMap.has(slotId)) stateMap.set(slotId, { mode, entries: [] })
    }
  } else {
    for (const e of la.entries) {
      if (!stateMap.has(e.homeSlotId)) stateMap.set(e.homeSlotId, { mode: e.mode, entries: [] })
      if (e.partnerCourseId && !e.isDeleted) stateMap.get(e.homeSlotId)!.entries.push(e)
    }
  }

  const deletedBySlot = new Map<string, LaEntryLine[]>()
  if (mappingEntries.length === 0) {
    for (const e of la.entries) {
      if (!e.isDeleted || !e.partnerCourseId) continue
      if (!deletedBySlot.has(e.homeSlotId)) deletedBySlot.set(e.homeSlotId, [])
      deletedBySlot.get(e.homeSlotId)!.push({
        code: e.partnerCourseCode ?? '',
        name: e.partnerCourseName ?? '',
        nameHr: e.partnerCourseNameHr,
        ects: e.awardedEcts ?? 0,
        deleted: true,
      })
    }
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

  const rowHeights: number[] = [20, 6, 20]
  let cursor = 4

  for (let sem = 1; sem <= 4; sem++) {
    const semSlots = la.slots.filter(s => s.semester === sem)

    const slotData = semSlots.map(slot => {
      const state = stateMap.get(slot.id)
      const isvuCode = slot.courseIsvuCode ?? slot.courseGroupIsvuCode
      const name = slot.courseName ?? slot.courseGroupName ?? ''
      const liveEntries: LaEntryLine[] = (state?.entries ?? []).map(m => ({
        code: m.partnerCourseCode ?? '',
        name: m.partnerCourseName ?? '',
        nameHr: m.partnerCourseNameHr,
        ects: m.awardedEcts ?? 0,
        deleted: m.enrollmentStatus === 'NotPassed',
      }))
      const rows = [...(deletedBySlot.get(slot.id) ?? []), ...liveEntries]
      return { slot, state, isvuCode, name, rows }
    })

    const maxEntryRows = slotData.reduce((max, sd) => Math.max(max, sd.rows.length), 0)
    const headerCodeRow = cursor
    const headerNameRow = cursor + 1
    const totalSemRows = 2 + maxEntryRows * 2

    ws[`A${headerCodeRow}`] = c(sem, { bold: true, bg: HEADER_BG, halign: 'center', valign: 'middle' })
    for (let r = headerCodeRow + 1; r < headerCodeRow + totalSemRows; r++) {
      ws[`A${r}`] = c('', { bg: HEADER_BG })
    }
    if (totalSemRows > 1) merges.push({ s: { r: headerCodeRow - 1, c: 0 }, e: { r: headerCodeRow - 1 + totalSemRows - 1, c: 0 } })

    for (let r = headerCodeRow; r < headerCodeRow + totalSemRows; r++) {
      for (let pos = 1; pos <= TOTAL_COLS; pos++) ws[`${colLetter(pos)}${r}`] = empty(undefined, true)
    }

    for (const sd of slotData) {
      const { slot, state, isvuCode, name, rows } = sd
      const slotBg = slot.color.replace('#', '')
      const startCol = slot.slotPosition
      const endCol = slot.slotPosition + slot.ects - 1
      const colWidthChars = Math.max(1, slot.ects) * CHARS_PER_COL

      const outlineColor = state?.mode ? MODE_OUTLINE_COLOR[state.mode] : undefined
      const outlineBorder = outlineColor
        ? { style: 'medium' as const, color: { rgb: outlineColor } }
        : { style: 'thin' as const, color: { rgb: 'BFBFBF' } }
      const thinBorder = { style: 'thin' as const, color: { rgb: 'BFBFBF' } }
      const noBorder = { style: 'none' as const }
      const headerNameBottomBorder = maxEntryRows === 0 ? outlineBorder : noBorder

      function writeRow(row: number, text: string, opts: { bold?: boolean; strike?: boolean; color?: string; top: typeof outlineBorder | typeof noBorder; bottom: typeof outlineBorder | typeof noBorder }) {
        const style = {
          font: { name: FONT, sz: 9, bold: !!opts.bold, strike: !!opts.strike, color: { rgb: opts.color ?? '000000' } },
          fill: { fgColor: { rgb: slotBg } },
          alignment: { wrapText: true, horizontal: 'left' as const, vertical: 'top' as const },
          border: { top: opts.top, bottom: opts.bottom, left: outlineBorder, right: outlineBorder },
        }
        ws[`${colLetter(startCol)}${row}`] = { v: text, t: 's', s: style }
        for (let pos = startCol + 1; pos <= endCol; pos++) {
          ws[`${colLetter(pos)}${row}`] = {
            v: '', t: 's',
            s: {
              fill: { fgColor: { rgb: slotBg } },
              border: { top: opts.top, bottom: opts.bottom, right: pos === endCol ? outlineBorder : undefined },
              alignment: { vertical: 'top' as const },
            },
          }
        }
        if (endCol > startCol) merges.push({ s: { r: row - 1, c: startCol }, e: { r: row - 1, c: endCol } })
      }

      writeRow(headerCodeRow, isvuCode ? String(isvuCode) : '', { bold: true, top: outlineBorder, bottom: noBorder })
      writeRow(headerNameRow, name, { top: noBorder, bottom: headerNameBottomBorder })

      for (let i = 0; i < maxEntryRows; i++) {
        const codeRow = headerNameRow + 1 + i * 2
        const detailsRow = codeRow + 1
        const entry = rows[i]
        const isLastEntry = i === maxEntryRows - 1
        const detailsBottom = isLastEntry ? outlineBorder : thinBorder

        if (!entry) {
          writeRow(codeRow, '', { top: noBorder, bottom: noBorder })
          writeRow(detailsRow, '', { top: noBorder, bottom: detailsBottom })
          continue
        }

        const detailsLines = [entry.name]
        if (entry.nameHr) detailsLines.push(entry.nameHr)
        detailsLines.push(`${entry.ects} ECTS`, '')

        writeRow(codeRow, entry.code, { bold: true, strike: entry.deleted, color: entry.deleted ? 'CC0000' : '000000', top: noBorder, bottom: noBorder })
        writeRow(detailsRow, detailsLines.join('\n'), { strike: entry.deleted, color: entry.deleted ? 'CC0000' : '000000', top: noBorder, bottom: detailsBottom })
      }
    }

    const headerNameLines = slotData.reduce((max, sd) => Math.max(max, wrappedLineCount(sd.name, Math.max(1, sd.slot.ects) * CHARS_PER_COL)), 1)
    rowHeights.push(14)
    rowHeights.push(Math.max(16, headerNameLines * LINE_HEIGHT_PT))
    for (let i = 0; i < maxEntryRows; i++) {
      let detailsPt = DEFAULT_ENTRY_ROW_PT
      for (const sd of slotData) {
        const entry = sd.rows[i]
        if (entry) detailsPt = Math.max(detailsPt, entryRowHeightPt(entry, sd.slot.ects))
      }
      rowHeights.push(14)
      rowHeights.push(detailsPt)
    }
    cursor += totalSemRows
  }

  const spacerRow = cursor
  cursor += 1

  const LEGEND_ENTRIES = [
    { label: tr('laAtHome', lang), swatchBg: '4472C4' },
  ]

  for (let ci = 0; ci <= TOTAL_COLS; ci++) ws[`${colLetter(ci)}${spacerRow}`] = empty(undefined, false)
  rowHeights.push(6)

  LEGEND_ENTRIES.forEach(({ label, swatchBg }, i) => {
    const r = cursor + i
    ws[`A${r}`] = empty(undefined, false)
    ws[`B${r}`] = c('  ', { bg: swatchBg, borders: true })
    ws[`C${r}`] = c(label, { borders: false, sz: 9 })
    for (let ci = 3; ci <= 6; ci++) ws[`${colLetter(ci)}${r}`] = empty(undefined, false)
    merges.push({ s: { r: r - 1, c: 2 }, e: { r: r - 1, c: 6 } })
    rowHeights.push(14)
  })

  const lastRow = cursor + LEGEND_ENTRIES.length - 1

  ws['!ref'] = `A1:${colLetter(TOTAL_COLS)}${lastRow}`
  ws['!merges'] = merges

  ws['!cols'] = [
    { wch: 7 },
    ...Array(30).fill({ wch: 5.5 }),
  ]

  ws['!rows'] = rowHeights.map(hpt => ({ hpt }))

  return ws
}

// Main export function

export function exportExchangeExcel(
  recognition: RecognitionResponse,
  mappingScheme: MappingSchemeResponse,
  la: LearningAgreementResponse,
  exchange: ExchangeResponse,
  locale: string = 'hr',
): void {
  const lang: Lang = locale === 'en' ? 'en' : 'hr'
  const wb = XLSX.utils.book_new()

  const bottomEntries: ExportEntry[] = (
    mappingScheme.entries.length > 0 ? mappingScheme.entries : recognition.entries
  )
    .slice()
    .sort((a, b) => a.partnerCourseName.localeCompare(b.partnerCourseName))

  const wsRecognition = buildRecognitionSheet(bottomEntries, exchange, lang)
  XLSX.utils.book_append_sheet(wb, wsRecognition, tr('sheetRecognition', lang))

  const wsLA = buildLASheet(la, mappingScheme.entries, exchange, lang)
  XLSX.utils.book_append_sheet(wb, wsLA, tr('sheetLA', lang))

  const studentName = exchange.studentName.replace(/\s+/g, '_')
  const year = exchange.academicYear.replace('/', '-')
  XLSX.writeFile(wb, `Razmjena_${studentName}_${year}.xlsx`)
}
