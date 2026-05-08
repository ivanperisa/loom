import { ref } from 'vue'

interface ConfirmOptions {
  title: string
  message?: string
  confirmLabel?: string
  cancelLabel?: string
}

interface ConfirmState extends ConfirmOptions {
  resolve: (value: boolean) => void
}

const state = ref<ConfirmState | null>(null)

export function useConfirm() {
  function confirm(options: ConfirmOptions): Promise<boolean> {
    return new Promise((resolve) => {
      state.value = { ...options, resolve }
    })
  }

  function respond(value: boolean) {
    state.value?.resolve(value)
    state.value = null
  }

  return { state, confirm, respond }
}
