import { describe, expect, it } from 'vitest'
import { scrubAccessToken } from '@/errorTracking'

describe('error tracking', () => {
  it('never sends an access link token', () => {
    expect(scrubAccessToken('https://loom.example/access/AbC-123_xyz?x=1')).toBe('https://loom.example/access/[token]?x=1')
    expect(scrubAccessToken('/access/AbC-123_xyz')).toBe('/access/[token]')
    expect(scrubAccessToken('/exchange/00000000-0000-4000-8000-000000000006')).toBe('/exchange/00000000-0000-4000-8000-000000000006')
  })
})
