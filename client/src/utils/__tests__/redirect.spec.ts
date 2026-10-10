import { describe, expect, it } from 'vitest'
import { safeRedirect } from '@/utils/redirect'

describe('safeRedirect', () => {
  it('keeps paths inside the app', () => {
    expect(safeRedirect('/exchange/abc?tab=la')).toBe('/exchange/abc?tab=la')
  })

  it.each(['//evil.example', '/\\evil.example', 'https://evil.example', 'exchange', '', null, ['/home']])(
    'refuses %j',
    (value) => expect(safeRedirect(value)).toBeNull(),
  )
})
