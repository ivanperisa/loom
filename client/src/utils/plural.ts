export function nWord(
  n: number,
  locale: string,
  forms: { en: [string, string]; hr: [string, string, string] },
): string {
  if (locale === 'hr') {
    const [one, few, many] = forms.hr
    const mod100 = n % 100
    const mod10 = n % 10
    if (mod100 >= 11 && mod100 <= 14) return `${n} ${many}`
    if (mod10 === 1) return `${n} ${one}`
    if (mod10 >= 2 && mod10 <= 4) return `${n} ${few}`
    return `${n} ${many}`
  }
  const [one, many] = forms.en
  return n === 1 ? `${n} ${one}` : `${n} ${many}`
}
