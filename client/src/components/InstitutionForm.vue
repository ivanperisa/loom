<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { institutionService } from '@/services/institution.service'
import type { InstitutionResponse, StudyProgramResponse, StudyProfileResponse } from '@/types/institution.types'
import type { LocalInstitutionEntry } from '@/types/onboarding.types'

const props = defineProps<{
  role: 'Student' | 'Coordinator' | 'Admin'
  prefill?: LocalInstitutionEntry
  submitLabel: string
}>()

const emit = defineEmits<{
  submit: [entry: LocalInstitutionEntry]
  cancel: []
}>()

const { t, locale } = useI18n()

const mode = ref<'existing' | 'new'>('existing')
const institutions = ref<InstitutionResponse[]>([])
const programs = ref<StudyProgramResponse[]>([])
const profiles = ref<StudyProfileResponse[]>([])

const selectedInstitutionId = ref<string | null>(null)
const selectedProgramId = ref<string | null>(null)
const selectedProfileId = ref<string | null>(null)
const useNewProfile = ref(false)
const newProfileName = ref('')

const newInstitutionName = ref('')
const newInstitutionCountry = ref('')
const newInstitutionCity = ref('')
const newInstitutionErasmusCode = ref('')
const newInstitutionIscedCode = ref('')
const newInstitutionProgramName = ref('')
const newInstitutionProfileName = ref('')
const newInstitutionLevel = ref('Undergraduate')
const newInstitutionDurationSemesters = ref<number | undefined>(undefined)

const isStudent = computed(() => props.role === 'Student')
const loadError = ref<string | null>(null)

function localizedName(name: string, nameEn?: string) {
  return locale.value === 'en' ? nameEn || name : name || nameEn || ''
}

async function loadInstitutions() {
  try {
    loadError.value = null
    const response = await institutionService.getHomeInstitutions()
    institutions.value = response.data ?? []
  } catch {
    loadError.value = t('errors.unexpected')
  }
}

async function loadPrograms(institutionId: string) {
  try {
    loadError.value = null
    const response = await institutionService.getProgramsByInstitution(institutionId)
    programs.value = response.data ?? []
  } catch {
    loadError.value = t('errors.unexpected')
  }
}

async function loadProfiles(programId: string) {
  if (!selectedInstitutionId.value) {
    profiles.value = []
    return
  }

  try {
    loadError.value = null
    const response = await institutionService.getProfilesByProgram(selectedInstitutionId.value, programId)
    profiles.value = response.data ?? []
  } catch {
    loadError.value = t('errors.unexpected')
  }
}

async function onInstitutionChanged() {
  selectedProgramId.value = null
  selectedProfileId.value = null
  useNewProfile.value = false
  newProfileName.value = ''
  programs.value = []
  profiles.value = []

  if (selectedInstitutionId.value && isStudent.value) {
    await loadPrograms(selectedInstitutionId.value)
  }
}

async function onProgramChanged() {
  selectedProfileId.value = null
  useNewProfile.value = false
  newProfileName.value = ''
  profiles.value = []

  if (selectedProgramId.value) {
    await loadProfiles(selectedProgramId.value)
  }
}

const canSubmit = computed(() => {
  if (mode.value === 'new') {
    if (!newInstitutionName.value.trim() || !newInstitutionCountry.value.trim()) {
      return false
    }

    if (!isStudent.value) {
      return true
    }

    return Boolean(
      newInstitutionIscedCode.value.trim() &&
        newInstitutionProgramName.value.trim() &&
        newInstitutionProfileName.value.trim()
    )
  }

  if (!selectedInstitutionId.value) {
    return false
  }

  if (!isStudent.value) {
    return true
  }

  if (!selectedProgramId.value) {
    return false
  }

  if (useNewProfile.value) {
    return newProfileName.value.trim().length > 0
  }

  return selectedProfileId.value !== null
})

function submit() {
  if (!canSubmit.value) {
    return
  }

  if (mode.value === 'new') {
    emit('submit', {
      institutionName: newInstitutionName.value.trim(),
      programName: isStudent.value ? newInstitutionProgramName.value.trim() : undefined,
      profileName: isStudent.value ? newInstitutionProfileName.value.trim() : undefined,
      newInstitution: {
        name: newInstitutionName.value.trim(),
        country: newInstitutionCountry.value.trim(),
        city: newInstitutionCity.value.trim() || null,
        erasmusCode: newInstitutionErasmusCode.value.trim() || null,
        iscedCode: isStudent.value ? newInstitutionIscedCode.value.trim() : null,
        programName: isStudent.value ? newInstitutionProgramName.value.trim() : null,
        profileName: isStudent.value ? newInstitutionProfileName.value.trim() : null,
        level: isStudent.value ? newInstitutionLevel.value : null,
        durationSemesters: isStudent.value ? (newInstitutionDurationSemesters.value ?? 0) : null
      }
    })
    return
  }

  const institution = institutions.value.find((i) => i.id === selectedInstitutionId.value)
  const program = programs.value.find((p) => p.id === selectedProgramId.value)
  const profile = profiles.value.find((p) => p.id === selectedProfileId.value)

  if (!institution) {
    return
  }

  if (!isStudent.value) {
    emit('submit', {
      institutionName: localizedName(institution.name, institution.nameEn),
      existingInstitutionId: institution.id
    })
    return
  }

  if (useNewProfile.value && selectedProgramId.value) {
    emit('submit', {
      institutionName: localizedName(institution.name, institution.nameEn),
      programName: program ? localizedName(program.name, program.nameEn) : undefined,
      profileName: newProfileName.value.trim(),
      existingInstitutionId: institution.id,
      newStudyProfile: {
        studyProgramId: selectedProgramId.value,
        profileName: newProfileName.value.trim()
      }
    })
    return
  }

  if (profile) {
    emit('submit', {
      institutionName: localizedName(institution.name, institution.nameEn),
      programName: program ? localizedName(program.name, program.nameEn) : undefined,
      profileName: localizedName(profile.name, profile.nameEn),
      existingStudyProfileId: profile.id
    })
  }
}

async function applyPrefill() {
  const prefill = props.prefill
  if (!prefill) {
    return
  }

  mode.value = prefill.newInstitution ? 'new' : 'existing'

  if (prefill.newInstitution) {
    newInstitutionName.value = prefill.newInstitution.name
    newInstitutionCountry.value = prefill.newInstitution.country
    newInstitutionCity.value = prefill.newInstitution.city ?? ''
    newInstitutionErasmusCode.value = prefill.newInstitution.erasmusCode ?? ''
    newInstitutionIscedCode.value = prefill.newInstitution.iscedCode ?? ''
    newInstitutionProgramName.value = prefill.newInstitution.programName ?? ''
    newInstitutionProfileName.value = prefill.newInstitution.profileName ?? ''
    return
  }

  if (prefill.existingInstitutionId) {
    selectedInstitutionId.value = prefill.existingInstitutionId
    try {
      await onInstitutionChanged()
      if (prefill.existingProgramId || prefill.newStudyProfile?.studyProgramId) {
        selectedProgramId.value = prefill.existingProgramId ?? prefill.newStudyProfile?.studyProgramId ?? null
        await onProgramChanged()
        if (prefill.newStudyProfile) {
          useNewProfile.value = true
          newProfileName.value = prefill.newStudyProfile.profileName ?? ''
        } else if (prefill.existingStudyProfileId) {
          selectedProfileId.value = prefill.existingStudyProfileId
        }
      }
    } catch {
      loadError.value = t('errors.unexpected')
    }
  }
}

watch(
  () => props.prefill,
  () => {
    applyPrefill()
  },
  { deep: true }
)

onMounted(async () => {
  await loadInstitutions()
  applyPrefill()
})
</script>

<template>
  <div class="institution-form-wrapper space-y-4 rounded-[14px] border p-5">
    <div class="flex flex-wrap gap-2">
      <button
        type="button"
        class="mode-pill"
        :class="mode === 'existing' ? 'mode-pill-active' : 'mode-pill-idle'"
        @click="mode = 'existing'"
      >
        {{ t('onboarding.institutions.existingOption') }}
      </button>
      <button
        type="button"
        class="mode-pill"
        :class="mode === 'new' ? 'mode-pill-active' : 'mode-pill-idle'"
        @click="mode = 'new'"
      >
        {{ t('onboarding.institutions.newOption') }}
      </button>
    </div>

    <div v-if="mode === 'existing'" class="space-y-4">
      <div>
        <label class="form-label">{{ t('onboarding.institutions.searchPlaceholder') }}</label>
        <select v-model="selectedInstitutionId" class="form-control" @change="onInstitutionChanged">
          <option :value="null">{{ t('onboarding.institutions.searchPlaceholder') }}</option>
          <option v-for="institution in institutions" :key="institution.id" :value="institution.id">
            {{ localizedName(institution.name, institution.nameEn) }} - {{ institution.country }}
          </option>
        </select>
      </div>

      <template v-if="isStudent && selectedInstitutionId">
        <div>
          <label class="form-label">{{ t('onboarding.institutions.selectProgram') }}</label>
          <select v-model="selectedProgramId" class="form-control" @change="onProgramChanged">
            <option :value="null">{{ t('onboarding.institutions.selectProgram') }}</option>
            <option v-for="program in programs" :key="program.id" :value="program.id">
              {{ localizedName(program.name, program.nameEn) }}
            </option>
          </select>
        </div>

        <div v-if="selectedProgramId">
          <template v-if="!useNewProfile">
            <label class="form-label">{{ t('onboarding.institutions.selectProfile') }}</label>
            <select v-model="selectedProfileId" class="form-control">
              <option :value="null">{{ t('onboarding.institutions.selectProfile') }}</option>
              <option v-for="profile in profiles" :key="profile.id" :value="profile.id">
                {{ localizedName(profile.name, profile.nameEn) }}
              </option>
            </select>
          </template>
          <input
            v-else
            v-model="newProfileName"
            class="form-control"
            :placeholder="t('onboarding.institutions.newProfilePlaceholder')"
          />
          <button type="button" class="mode-pill mode-pill-idle mt-2" @click="useNewProfile = !useNewProfile">
            {{ useNewProfile ? t('onboarding.institutions.selectProfile') : t('onboarding.institutions.newProfileOption') }}
          </button>
        </div>
      </template>
    </div>

    <div v-else class="grid gap-3 sm:grid-cols-2">
      <div>
        <label class="form-label">{{ t('onboarding.institutions.form.name') }}</label>
        <input v-model="newInstitutionName" class="form-control" :placeholder="`${t('onboarding.institutions.form.name')} *`" />
      </div>
      <div>
        <label class="form-label">{{ t('onboarding.institutions.form.country') }}</label>
        <input v-model="newInstitutionCountry" class="form-control" :placeholder="`${t('onboarding.institutions.form.country')} *`" />
      </div>
      <div>
        <label class="form-label">{{ t('onboarding.institutions.form.city') }}</label>
        <input v-model="newInstitutionCity" class="form-control" :placeholder="t('onboarding.institutions.form.city')" />
      </div>
      <div>
        <label class="form-label">{{ t('onboarding.institutions.form.erasmusCode') }}</label>
        <input v-model="newInstitutionErasmusCode" class="form-control" :placeholder="t('onboarding.institutions.form.erasmusCode')" />
      </div>

      <template v-if="isStudent">
        <div>
          <label class="form-label">{{ t('onboarding.institutions.form.iscedCode') }}</label>
          <input v-model="newInstitutionIscedCode" class="form-control" :placeholder="`${t('onboarding.institutions.form.iscedCode')} *`" />
        </div>
        <div>
          <label class="form-label">{{ t('onboarding.institutions.form.programName') }}</label>
          <input v-model="newInstitutionProgramName" class="form-control" :placeholder="`${t('onboarding.institutions.form.programName')} *`" />
        </div>
        <div class="sm:col-span-2">
          <label class="form-label">{{ t('onboarding.institutions.form.profileName') }}</label>
          <input v-model="newInstitutionProfileName" class="form-control" :placeholder="`${t('onboarding.institutions.form.profileName')} *`" />
        </div>
        <div>
          <label class="form-label">{{ t('onboarding.institutions.form.level') }}</label>
          <select v-model="newInstitutionLevel" class="form-control">
            <option value="Undergraduate">{{ t('studyProgramLevels.Undergraduate') }}</option>
            <option value="Graduate">{{ t('studyProgramLevels.Graduate') }}</option>
            <option value="Postgraduate">{{ t('studyProgramLevels.Postgraduate') }}</option>
          </select>
        </div>
        <div>
          <label class="form-label">{{ t('onboarding.institutions.form.durationSemesters') }}</label>
          <input v-model.number="newInstitutionDurationSemesters" type="number" min="1" max="20" class="form-control" />
        </div>
      </template>
    </div>

    <p v-if="loadError" class="text-red-400 text-sm mt-2">{{ loadError }}</p>

    <div class="flex flex-wrap gap-2">
      <button type="button" class="cta-btn" :disabled="!canSubmit" @click="submit">
        {{ submitLabel }}
      </button>
      <button v-if="prefill" type="button" class="cancel-btn" @click="emit('cancel')">
        {{ t('settings.institutions.cancel') }}
      </button>
    </div>
  </div>
</template>

<style scoped>
.institution-form-wrapper {
  background: rgba(33, 140, 217, 0.05);
  border-color: rgba(33, 140, 217, 0.2);
}

.form-label {
  margin-bottom: 6px;
  display: block;
  color: #8ac4ed;
  font-size: 0.8rem;
  font-weight: 500;
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.form-control {
  width: 100%;
  border-radius: 8px;
  border: 1px solid rgba(202, 228, 247, 0.15);
  background: rgba(255, 255, 255, 0.05);
  color: var(--color-light);
  padding: 10px 14px;
  font-size: 0.9rem;
  transition: border-color 0.15s ease, box-shadow 0.15s ease;
}

.form-control option {
  background: #0a2235;
  color: var(--color-light);
}

.form-control::placeholder {
  color: rgba(202, 228, 247, 0.4);
  opacity: 1;
}

.form-control:focus {
  border-color: #218cd9;
  outline: none;
  box-shadow: 0 0 0 3px rgba(33, 140, 217, 0.15);
}

.mode-pill {
  border-radius: 999px;
  border: 1px solid;
  padding: 8px 14px;
  font-size: 0.85rem;
  transition: all 0.15s ease;
}

.mode-pill-idle {
  border-color: rgba(202, 228, 247, 0.2);
  color: rgba(202, 228, 247, 0.5);
  background: transparent;
}

.mode-pill-active {
  border-color: #218cd9;
  color: #cae4f7;
  background: rgba(33, 140, 217, 0.15);
}

.cta-btn {
  border: none;
  border-radius: 10px;
  padding: 10px 24px;
  color: #ffffff;
  font-weight: 600;
  background: linear-gradient(135deg, #218cd9, #1a7abf);
  box-shadow: 0 4px 12px rgba(33, 140, 217, 0.3);
  transition: all 0.15s ease;
}

.cta-btn:hover:enabled {
  filter: brightness(1.1);
  box-shadow: 0 6px 16px rgba(33, 140, 217, 0.4);
}

.cta-btn:active:enabled {
  transform: scale(0.98);
}

.cta-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.cancel-btn {
  border: 1px solid rgba(202, 228, 247, 0.15);
  border-radius: 10px;
  background: transparent;
  color: rgba(202, 228, 247, 0.6);
  padding: 10px 24px;
  transition: all 0.15s ease;
}

.cancel-btn:hover {
  border-color: rgba(202, 228, 247, 0.3);
  color: rgba(202, 228, 247, 0.9);
}
</style>
