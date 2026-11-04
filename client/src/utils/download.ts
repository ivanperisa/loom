import type { AxiosResponse } from 'axios'

export function saveBlob(data: Blob, fileName: string) {
  const url = URL.createObjectURL(data)
  const a = document.createElement('a')
  a.href = url
  a.download = fileName
  a.click()
  URL.revokeObjectURL(url)
}

/** Saves a blob response under the name the server gave it (Content-Disposition), else `fallback`. */
export function saveResponse(res: AxiosResponse<Blob>, fallback: string) {
  const disposition = String(res.headers['content-disposition'] ?? '')
  const encoded = /filename\*=UTF-8''([^;]+)/i.exec(disposition)?.[1]
  const plain = /filename="?([^";]+)"?/i.exec(disposition)?.[1]
  saveBlob(res.data, encoded ? decodeURIComponent(encoded) : (plain ?? fallback))
}
