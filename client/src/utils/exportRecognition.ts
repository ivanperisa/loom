import XLSX from 'xlsx-js-style'
import type { RecognitionResponse } from '@/types/recognition.types'
import type { ExchangeResponse, LearningAgreementResponse, SlotStateResponse } from '@/types/exchange.types'

// --- Style helpers ---

type XlsxCell = { v: string | number; t?: string; s?: object }

const FONT = 'Calibri'
const HEADER_BG = 'D9D9D9'
const TITLE_BG = '2E4057'

function thin() {
  return { style: 'thin', color: { rgb: '000000' } }
}

function border() {
  return { top: thin(), bottom: thin(), left: thin(), right: thin() }
}

function c(
  value: string | number | null | undefined,
  opts: {
    bg?: string
    bold?: boolean
    sz?: number
    wrap?: boolean
    halign?: 'left' | 'center' | 'right'
    valign?: 'top' | 'middle' | 'bottom'
    color?: string
    borders?: boolean
    italic?: boolean
  } = {},
): XlsxCell {
  const {
    bg,
    bold = false,
    sz = 9,
    wrap = false,
    halign = 'left',
    valign = 'middle',
    color = '000000',
    borders = true,
    italic = false,
  } = opts
  return {
    v: value ?? '',
    t: typeof value === 'number' ? 'n' : 's',
    s: {
      font: { name: FONT, sz, bold, italic, color: { rgb: color } },
      alignment: { wrapText: wrap, horizontal: halign, vertical: valign },
      ...(bg ? { fill: { fgColor: { rgb: bg.replace('#', '') } } } : {}),
      ...(borders ? { border: border() } : {}),
    },
  }
}

function empty(bg?: string): XlsxCell {
  return c('', { bg, borders: true })
}

// --- Column index helpers ---

function colLetter(idx: number): string {
  let s = ''
  let n = idx + 1
  while (n > 0) {
    const r = (n - 1) % 26
    s = String.fromCharCode(65 + r) + s
    n = Math.floor((n - 1) / 26)
  }
  return s
}

function ref(r: number, c: number): string {
  return `${colLetter(c)}${r + 1}`
}

// --- Sheet 1: Recognition (ISVU obrazac) ---

function buildRecognitionSheet(
  recognition: RecognitionResponse,
  exchange: ExchangeResponse,
): { ws: Record<string, XlsxCell>; merges: object[] } {
  const ws: Record<string, XlsxCell> = {}
  const merges: object[] = []

  // Row 1: Title
  ws['A1'] = c('ISVU-obrazac za priznavanje predmeta ERASMUS-studentima', {
    bold: true, sz: 12, bg: TITLE_BG, color: 'FFFFFF', halign: 'center', borders: false,
  })
  for (const col of ['B', 'C', 'D', 'E', 'F', 'G']) {
    ws[`${col}1`] = c('', { bg: TITLE_BG, borders: false })
  }
  merges.push({ s: { r: 0, c: 0 }, e: { r: 0, c: 6 } })

  // Row 2: empty spacer
  ws['A2'] = c('')

  // Helper to write a label+value pair: label spans A-C, value spans D-G
  function labelRow(row: number, label: string, value: string | null) {
    ws[`A${row}`] = c(label, { bold: true, bg: HEADER_BG })
    ws[`B${row}`] = c('', { bg: HEADER_BG })
    ws[`C${row}`] = c('', { bg: HEADER_BG })
    merges.push({ s: { r: row - 1, c: 0 }, e: { r: row - 1, c: 2 } })
    ws[`D${row}`] = c(value ?? '', { wrap: true })
    ws[`E${row}`] = c('')
    ws[`F${row}`] = c('')
    ws[`G${row}`] = c('')
    merges.push({ s: { r: row - 1, c: 3 }, e: { r: row - 1, c: 6 } })
  }

  labelRow(3, 'Student:', exchange.studentName)
  labelRow(4, 'JMBAG:', exchange.studentJmbag)
  labelRow(5, 'Studij (prediplomski/diplomski):', 'diplomski')
  labelRow(6, 'Semestar:', String(exchange.studySemester))
  labelRow(7, 'Profil (za diplomski):', exchange.studyProfile.name)
  labelRow(8, 'Sveučilište razmjene:', exchange.foreignProgram.institutionName)
  labelRow(9, 'Fakultet razmjene:', exchange.foreignProgram.name)
  labelRow(10, 'Ak. god. razmjene:', exchange.academicYear)
  labelRow(11, 'Semestar razmjene (zimski/ljetni):', exchange.semesterType === 'Winter' ? 'zimski' : 'ljetni')
  labelRow(12, 'Mentor:', exchange.mentor)

  // Row 13: spacer
  ws['A13'] = c('')

  // Row 14: section header
  ws['A14'] = c('Predmeti koji se priznaju za druge predmete/obveze iz nastavnog programa', {
    bold: true, bg: HEADER_BG, halign: 'center',
  })
  for (let ci = 1; ci <= 7; ci++) ws[`${colLetter(ci)}14`] = c('', { bg: HEADER_BG })
  merges.push({ s: { r: 13, c: 0 }, e: { r: 13, c: 7 } })

  // Row 15: spacer
  ws['A15'] = c('')

  // Row 16: column headers
  const headers = [
    'Šifra predmeta', 'Naziv engleski', 'Status predmeta', 'Naziv - hrvatski',
    'Sati (P/A/L)', 'ECTS', 'Rbr.', 'Prizaje se za predmet - Naziv',
    'Izb. grupa', 'Naziv izb. grupe', 'Semestar', 'Priznato ECTS-a',
    'Ocjena\noriginalna', 'Ocjena\nECTS\n(F-A)', 'Ocjena\nhrv.\n(1-5)', 'Datum polaganja',
  ]
  headers.forEach((h, i) => {
    ws[`${colLetter(i)}16`] = c(h, { bold: true, bg: HEADER_BG, wrap: true, halign: 'center', valign: 'middle' })
  })

  // Rows 17+: data
  let row = 17
  for (const entry of recognition.entries) {
    const bg = entry.courseSlotColor.replace('#', '')
    ws[`A${row}`] = c(entry.foreignCourseCode, { bg })
    ws[`B${row}`] = c(entry.foreignCourseNameEn, { bg, wrap: true })
    ws[`C${row}`] = c(entry.enrollmentStatus, { bg })
    ws[`D${row}`] = c(entry.foreignCourseNameHr, { bg, wrap: true })
    ws[`E${row}`] = c(entry.foreignCourseHours, { bg })
    ws[`F${row}`] = c(entry.foreignCourseEcts, { bg })
    ws[`G${row}`] = c(row - 16, { bg, halign: 'center' })
    ws[`H${row}`] = c(entry.courseSlotName, { bg, wrap: true })
    ws[`I${row}`] = c(entry.courseSlotCode, { bg, halign: 'center' })
    ws[`J${row}`] = c(entry.courseSlotCategoryName, { bg, wrap: true })
    ws[`K${row}`] = c(entry.courseSlotSemester, { bg, halign: 'center' })
    ws[`L${row}`] = c(entry.awardedEcts, { bg, halign: 'center' })
    ws[`M${row}`] = c(entry.originalGrade, { bg })
    ws[`N${row}`] = c(entry.ectsGrade, { bg, halign: 'center' })
    ws[`O${row}`] = c(entry.hrGrade, { bg, halign: 'center' })
    ws[`P${row}`] = c(entry.examDate ?? '', { bg })
    row++
  }

  // UKUPNO row
  ws[`A${row}`] = c('UKUPNO', { bold: true, bg: HEADER_BG })
  for (let ci = 1; ci <= 11; ci++) ws[`${colLetter(ci)}${row}`] = c('', { bg: HEADER_BG })
  const totalEcts = recognition.entries.reduce((s, e) => s + e.awardedEcts, 0)
  ws[`L${row}`] = c(totalEcts, { bold: true, bg: HEADER_BG, halign: 'center' })
  for (let ci = 12; ci <= 15; ci++) ws[`${colLetter(ci)}${row}`] = c('', { bg: HEADER_BG })
  merges.push({ s: { r: row - 1, c: 0 }, e: { r: row - 1, c: 10 } })

  ws['!ref'] = `A1:P${row}`
  ws['!merges'] = merges
  ws['!cols'] = [
    { wch: 12 }, { wch: 40 }, { wch: 14 }, { wch: 38 }, { wch: 14 }, { wch: 6 },
    { wch: 5 }, { wch: 30 }, { wch: 10 }, { wch: 24 }, { wch: 8 }, { wch: 10 },
    { wch: 10 }, { wch: 8 }, { wch: 8 }, { wch: 12 },
  ]

  return { ws, merges }
}

// --- Sheet 2: Learning Agreement ---

function buildLASheet(
  la: LearningAgreementResponse,
): Record<string, XlsxCell> {
  const ws: Record<string, XlsxCell> = {}
  const merges: object[] = []
  const stateMap = new Map<string, SlotStateResponse>()
  for (const s of la.slotStates) stateMap.set(s.courseSlotId, s)

  const MODE_BORDER_COLOR: Record<string, string> = {
    AtHome: '808080',
    AtExchange: 'EA580C',
    AfterExchange: 'F59E0B',
  }

  // Row 1: header
  ws['A1'] = c('Learning Agreement - Tablica studijskog profila', {
    bold: true, sz: 11, bg: TITLE_BG, color: 'FFFFFF', halign: 'center', borders: false,
  })
  for (let ci = 1; ci <= 31; ci++) ws[`${colLetter(ci)}1`] = c('', { bg: TITLE_BG, borders: false })
  merges.push({ s: { r: 0, c: 0 }, e: { r: 0, c: 31 } })

  // Row 2: spacer
  ws['A2'] = c('')

  // Row 3: legend
  ws['A3'] = c('Legenda:', { bold: true, borders: false })
  const legend = [
    { label: 'Položeno na FER-u', color: '808080' },
    { label: 'Polaže na mobilnosti', color: 'EA580C' },
    { label: 'Polaže nakon mobilnosti', color: 'F59E0B' },
  ]
  legend.forEach((l, i) => {
    const col = colLetter(i * 4 + 1)
    ws[`${col}3`] = c('  ', {
      bg: l.color, borders: true,
    })
    ws[`${colLetter(i * 4 + 2)}3`] = c(l.label, { borders: false })
  })

  // Row 4: spacer
  ws['A4'] = c('')

  // Row 5: ECTS position header
  ws['A5'] = c('Sem.', { bold: true, bg: HEADER_BG, halign: 'center' })
  for (let pos = 1; pos <= 30; pos++) {
    ws[`${colLetter(pos)}5`] = c(pos, { bold: true, bg: HEADER_BG, halign: 'center', sz: 8 })
  }
  ws[`${colLetter(31)}5`] = c('Trans.', { bold: true, bg: HEADER_BG, halign: 'center', sz: 8 })

  // Rows 6-9: Semesters 1-4
  for (let sem = 1; sem <= 4; sem++) {
    const rowIdx = sem + 5 - 1  // row 6,7,8,9 (0-indexed: 5,6,7,8)
    const rowNum = sem + 5       // 1-based: 6,7,8,9

    // Column A: semester number
    ws[`A${rowNum}`] = c(sem, { bold: true, bg: HEADER_BG, halign: 'center', valign: 'middle' })

    // Fill default empty cells for all ECTS positions
    for (let pos = 1; pos <= 31; pos++) {
      ws[`${colLetter(pos)}${rowNum}`] = empty()
    }

    // Fill slots for this semester
    const semSlots = la.slots.filter(s => s.semester === sem)
    for (const slot of semSlots) {
      const state = stateMap.get(slot.id)
      const slotBg = slot.color.replace('#', '')
      const borderColor = state ? (MODE_BORDER_COLOR[state.mode] ?? '000000') : '000000'
      const modeBorder = state
        ? { style: 'medium', color: { rgb: borderColor } }
        : thin()

      // Build cell text
      let text = slot.courseName
      if (slot.courseCode) text = `${slot.courseCode}\n${slot.courseName}`
      if (state?.mappings?.length) {
        text += '\n' + state.mappings.map(m => `↳ ${m.foreignCourseCode} (${m.awardedEcts} ECTS)`).join('\n')
      }

      const startCol = slot.colStart      // 1-based ECTS position → column index = colStart
      const endCol = slot.colStart + slot.ects - 1

      // Set first cell with content
      ws[`${colLetter(startCol)}${rowNum}`] = {
        v: text,
        t: 's',
        s: {
          font: { name: FONT, sz: 7, bold: slot.categoryCode === 'Thesis' },
          fill: { fgColor: { rgb: slotBg } },
          alignment: { wrapText: true, horizontal: 'center', vertical: 'middle' },
          border: {
            top: modeBorder,
            bottom: modeBorder,
            left: modeBorder,
            right: modeBorder,
          },
        },
      }

      // Empty fill cells for span
      for (let pos = startCol + 1; pos <= endCol; pos++) {
        ws[`${colLetter(pos)}${rowNum}`] = {
          v: '',
          t: 's',
          s: {
            fill: { fgColor: { rgb: slotBg } },
            border: {
              top: modeBorder,
              bottom: modeBorder,
              left: modeBorder,
              right: modeBorder,
            },
          },
        }
      }

      if (endCol > startCol) {
        merges.push({ s: { r: rowIdx, c: startCol }, e: { r: rowIdx, c: endCol } })
      }
    }
  }

  const lastRow = 9
  ws['!ref'] = `A1:${colLetter(31)}${lastRow}`
  ws['!merges'] = merges
  ws['!cols'] = [
    { wch: 7 },
    ...Array(30).fill({ wch: 5.5 }),
    { wch: 8 },
  ]
  ws['!rows'] = [
    {}, {}, {}, {}, { hpt: 20 },
    { hpt: 60 }, { hpt: 60 }, { hpt: 60 }, { hpt: 60 },
  ]

  return ws
}

// --- Main export function ---

export function exportRecognitionExcel(
  recognition: RecognitionResponse,
  la: LearningAgreementResponse,
  exchange: ExchangeResponse,
): void {
  const wb = XLSX.utils.book_new()

  const { ws: wsRecognition } = buildRecognitionSheet(recognition, exchange)
  XLSX.utils.book_append_sheet(wb, wsRecognition, 'Priznavanje')

  const wsLA = buildLASheet(la)
  XLSX.utils.book_append_sheet(wb, wsLA, 'Learning Agreement')

  const studentName = exchange.studentName.replace(/\s+/g, '_')
  const year = exchange.academicYear.replace('/', '-')
  XLSX.writeFile(wb, `Razmjena_${studentName}_${year}.xlsx`)
}
