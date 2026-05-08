import { ref } from 'vue'

export type ToastType = 'success' | 'error' | 'warning' | 'info'

export interface Toast {
  id: string
  type: ToastType
  title: string
  message?: string
  duration: number
}

const toasts = ref<Toast[]>([])

function removeToast(id: string) {
  toasts.value = toasts.value.filter((t) => t.id !== id)
}

function addToast(toast: Omit<Toast, 'id'>) {
  const id = crypto.randomUUID()
  toasts.value.push({ ...toast, id })
  if (toast.duration > 0) {
    setTimeout(() => removeToast(id), toast.duration)
  }
}

export function useNotification() {
  return {
    toasts,
    addToast,
    removeToast,
    notifySuccess: (title: string, message?: string) =>
      addToast({ type: 'success', title, message, duration: 5000 }),
    notifyError: (title: string, message?: string) =>
      addToast({ type: 'error', title, message, duration: 7000 }),
    notifyWarning: (title: string, message?: string) =>
      addToast({ type: 'warning', title, message, duration: 5000 }),
    notifyInfo: (title: string, message?: string) =>
      addToast({ type: 'info', title, message, duration: 5000 }),
  }
}
