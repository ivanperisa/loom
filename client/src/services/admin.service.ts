import { api } from '@/services/api'
import type { MakeCoordinatorRequest } from '@/types/admin.types'

export const adminService = {
  makeCoordinator(request: MakeCoordinatorRequest) {
    return api.post('/admin/make-coordinator', request)
  }
}
