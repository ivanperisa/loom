import { readFileSync, readdirSync, statSync } from 'node:fs'
import { join, resolve } from 'node:path'
import { describe, expect, it } from 'vitest'
import en from '@/i18n/locales/en'
import hr from '@/i18n/locales/hr'

const root = resolve(__dirname, '../../..')

function files(dir: string, pattern: RegExp): string[] {
  return readdirSync(dir).flatMap((name) => {
    const path = join(dir, name)
    if (statSync(path).isDirectory()) return name === '__tests__' || name === 'node_modules' ? [] : files(path, pattern)
    return pattern.test(name) ? [path] : []
  })
}

function has(messages: unknown, key: string): boolean {
  const value = key.split('.').reduce<unknown>((node, part) => (node as Record<string, unknown> | undefined)?.[part], messages)
  return typeof value === 'string'
}

describe('messages', () => {
  it('every t("...") with a fixed key exists in both languages', () => {
    const missing: string[] = []
    for (const file of files(join(root, 'src'), /\.(vue|ts)$/)) {
      for (const [, key] of readFileSync(file, 'utf8').matchAll(/\bt\(\s*'([a-zA-Z][\w.]*)'/g)) {
        if (!has(en, key!) || !has(hr, key!)) missing.push(`${file.slice(root.length)}: ${key}`)
      }
    }
    expect(missing).toEqual([])
  })

  it('every error code the API can return has a translation', () => {
    const server = resolve(root, '../server')
    const codes = new Set<string>()
    for (const file of files(server, /Errors\.cs$|ApiExceptionHandler\.cs$/)) {
      for (const [, code] of readFileSync(file, 'utf8').matchAll(/"([A-Z][A-Z_]{3,})"/g)) codes.add(code!)
    }
    expect(codes.size).toBeGreaterThan(30)
    const untranslated = [...codes].filter((code) => !has(en, `apiErrors.codes.${code}`) || !has(hr, `apiErrors.codes.${code}`))
    expect(untranslated).toEqual([])
  })
})
