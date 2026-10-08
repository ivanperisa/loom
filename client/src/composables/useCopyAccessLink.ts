import { useI18n } from 'vue-i18n'
import { exchangeService } from '@/services/exchange.service'
import { useNotification } from '@/composables/useNotification'
import { buildAccessLink, copyPendingText } from '@/utils/accessLink'

/** Copy (or regenerate and copy) a placeholder exchange's access link. API errors are toasted by the api client. */
export function useCopyAccessLink() {
  const { t } = useI18n()
  const { notifySuccess, notifyWarning } = useNotification()

  async function copy(link: Promise<string>, copiedMessage: string, copyFailedMessage: string): Promise<boolean> {
    try {
      await copyPendingText(link)
      notifySuccess(copiedMessage)
      return true
    } catch {
      // Only warn about the clipboard when the link itself was fetched.
      const fetched = await link.then(() => true, () => false)
      if (fetched) notifyWarning(copyFailedMessage)
      return fetched
    }
  }

  const copyAccessLink = (exchangeGuid: string) =>
    copy(
      exchangeService.getAccessLink(exchangeGuid).then((res) => buildAccessLink(res.data.token)),
      t('exchangeAccess.linkCopied'),
      t('exchangeAccess.copyLinkFailed'),
    )

  /** Revokes the current link (open guest sessions end at once) and copies the new one. */
  const regenerateAccessLink = (exchangeGuid: string) =>
    copy(
      exchangeService.regenerateAccessLink(exchangeGuid).then((res) => buildAccessLink(res.data.token)),
      t('exchangeAccess.regenerated'),
      t('exchangeAccess.copyFailed'),
    )

  return { copyAccessLink, regenerateAccessLink }
}
