import { z } from 'zod'
import { ApiError } from '../../shared/api/client'
import type { SuggestionDetails, SuggestionInput } from '../../shared/api/contracts'
import { api } from '../../shared/api/resources'

const inputSchema = z.object({
  title: z.string(),
  currentFlaws: z.string(),
  proposedSolution: z.string(),
  improvementObject: z.string(),
  targetDepartmentId: z.string(),
  templateId: z.string().uuid().nullish(),
  isAnonymous: z.boolean().optional(),
})

export const composeSchema = inputSchema.extend({
  pendingDraftId: z.string().uuid().optional(),
  creation: z.object({ key: z.string().uuid(), input: inputSchema }).optional(),
})

export function readSuggestionSession<T>(key: string, schema: z.ZodType<T>): T | undefined {
  try {
    const parsed = schema.safeParse(JSON.parse(sessionStorage.getItem(key) ?? 'null'))
    return parsed.success ? parsed.data : undefined
  } catch {
    return undefined
  }
}

export function writeSuggestionSession(key: string, value?: unknown) {
  try {
    if (value === undefined) sessionStorage.removeItem(key)
    else sessionStorage.setItem(key, JSON.stringify(value))
  } catch {
    // The current screen remains usable when storage is unavailable.
  }
}

export function inputFromCard(card: SuggestionDetails): SuggestionInput {
  return {
    title: card.title,
    currentFlaws: card.currentFlaws,
    proposedSolution: card.proposedSolution,
    improvementObject: card.improvementObject,
    targetDepartmentId: card.targetDepartmentId,
    templateId: card.templateId,
    isAnonymous: card.isAnonymous || undefined,
  }
}

const submissionSchema = z.number().int().nonnegative()
const pendingSubmissions = new Map<string, number>()
const submissionKey = (userId: string, id: string) => `ideahub-submit-${userId}-${id}`
const receiptKey = (userId: string, id: string) => `ideahub-compose-receipt-${userId}-${id}`

export function isWriteRejection(error: unknown): error is ApiError {
  return (
    error instanceof ApiError && error.status >= 400 && error.status < 500 && error.status !== 408
  )
}

export function rememberDraftForSubmission(userId: string, id: string, composeKey: string) {
  writeSuggestionSession(receiptKey(userId, id), composeKey)
}

function clearPendingSubmission(key: string) {
  pendingSubmissions.delete(key)
  writeSuggestionSession(key)
}

function finishSubmission(userId: string, id: string) {
  clearPendingSubmission(submissionKey(userId, id))
  const key = receiptKey(userId, id)
  const composeKey = readSuggestionSession(key, z.string())
  if (composeKey && readSuggestionSession(composeKey, composeSchema)?.pendingDraftId === id)
    writeSuggestionSession(composeKey)
  writeSuggestionSession(key)
}

function readPendingSubmission(key: string) {
  return pendingSubmissions.get(key) ?? readSuggestionSession(key, submissionSchema)
}

export function confirmPendingSubmission(
  userId: string,
  card: SuggestionDetails,
  pending = readPendingSubmission(submissionKey(userId, card.id)),
) {
  if (!card.isMine || pending === undefined || card.submissionCount <= pending) return false
  finishSubmission(userId, card.id)
  return true
}

export async function submitWithRecovery(userId: string, id: string) {
  const key = submissionKey(userId, id)
  const pending = readPendingSubmission(key)
  const card = await api.suggestion(id)
  if (pending !== undefined && confirmPendingSubmission(userId, card, pending)) return
  if (!card.allowedActions.includes('submit')) {
    clearPendingSubmission(key)
    throw new ApiError(409, 'Предложение сейчас нельзя подать. Проверьте статус в карточке.')
  }

  pendingSubmissions.set(key, card.submissionCount)
  writeSuggestionSession(key, card.submissionCount)
  try {
    await api.submitSuggestion(id)
    finishSubmission(userId, id)
  } catch (error) {
    if (isWriteRejection(error) && error.status !== 409) {
      clearPendingSubmission(key)
    } else {
      const current = await api.suggestion(id).catch(() => null)
      if (current && confirmPendingSubmission(userId, current, card.submissionCount)) return
    }
    throw error
  }
}
