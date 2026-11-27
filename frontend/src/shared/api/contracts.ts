import { z } from 'zod'

export const statuses = [
  'Draft',
  'Submitted',
  'ReturnedByManager',
  'InCommission',
  'RevisionRequired',
  'Rejected',
  'ApprovedPlanning',
  'InImplementation',
  'Implemented',
] as const
export const suggestionSchema = z.object({
  id: z.string().uuid(),
  title: z.string(),
  status: z.enum(statuses),
  created: z.string(),
  allowedActions: z.array(z.string()).optional(),
  supportCount: z.number().int().nonnegative().optional(),
  supportedByMe: z.boolean().optional(),
})
const suggestionListItemSchema = suggestionSchema.extend({
  createdBy: z.string().nullable(),
  authorName: z.string().nullable(),
  targetDepartmentId: z.string().uuid(),
  departmentName: z.string().nullable(),
  improvementObject: z.string(),
  shortDescription: z.string(),
  allowedActions: z.array(z.enum(['edit', 'submit', 'moderate', 'commission', 'support'])),
})
export const suggestionSearchSchema = z.object({
  items: z.array(suggestionListItemSchema),
  totalCount: z.number().int().nonnegative(),
  pageNumber: z.number().int().positive(),
  pageSize: z.number().int().min(1).max(100),
})
const suggestionVersionSchema = z.string().length(64)
export const updateSuggestionResponseSchema = z.object({ version: suggestionVersionSchema })
export const suggestionDetailsSchema = suggestionListItemSchema
  .omit({ shortDescription: true })
  .extend({
    isMine: z.boolean(),
    isAnonymous: z.boolean(),
    currentFlaws: z.string(),
    proposedSolution: z.string(),
    templateId: z.string().uuid().nullable(),
    templateName: z.string().nullable(),
    templateIsActive: z.boolean().nullable(),
    registrationNumber: z.string().nullable(),
    commissionNote: z.string().nullable(),
    version: suggestionVersionSchema,
    submissionCount: z.number().int().nonnegative(),
    history: z.array(
      z.object({
        id: z.string().uuid(),
        occurredAt: z.string(),
        fromStatus: z.enum(statuses),
        toStatus: z.enum(statuses),
        note: z.string().nullable(),
      }),
    ),
  })
export const departmentSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  headUserId: z.string().nullable(),
  employeesCount: z.number().int().nonnegative(),
})
export const templateSchema = z.object({
  id: z.string().uuid(),
  type: z.enum(['Template', 'Bottleneck']),
  departmentId: z.string().uuid().nullable(),
  improvementObject: z.string(),
  title: z.string(),
  problemHint: z.string(),
  solutionHint: z.string(),
  isActive: z.boolean(),
})
export const profileSchema = z.object({
  id: z.string(),
  email: z.string(),
  fullName: z.string(),
  position: z.string(),
  departmentId: z.string().uuid().nullable(),
  roles: z.array(z.string()),
})
export const employeeDirectorySchema = suggestionSearchSchema.extend({
  items: z.array(profileSchema.extend({ departmentName: z.string().nullable() })),
})
export const serviceSettingsSchema = z.object({
  supportEnabled: z.boolean(),
  anonymousEnabled: z.boolean(),
  notificationsEnabled: z.boolean(),
  boardEnabled: z.boolean(),
  version: z.string().uuid(),
})
export const notificationSchema = z.object({
  id: z.string().uuid(),
  suggestionId: z.string().uuid(),
  title: z.string(),
  status: z.enum(statuses),
  occurredAt: z.string(),
})
export type Suggestion = z.infer<typeof suggestionSchema>
export type SuggestionDetails = z.infer<typeof suggestionDetailsSchema>
export type Department = z.infer<typeof departmentSchema>
export type Template = z.infer<typeof templateSchema>
export type Profile = z.infer<typeof profileSchema>
export type ServiceSettings = z.infer<typeof serviceSettingsSchema>
export type SuggestionInput = {
  title: string
  currentFlaws: string
  proposedSolution: string
  improvementObject: string
  targetDepartmentId: string
  templateId?: string | null
  isAnonymous?: boolean
}
export type UpdateSuggestionInput = SuggestionInput & { version: string }
export type SuggestionSearch = {
  scope: 'mine'
  search?: string
  status?: Suggestion['status']
  sort: 'createdDesc' | 'createdAsc' | 'title' | 'status'
  pageNumber: number
  pageSize: number
}
export type DepartmentInput = Omit<Department, 'id'>
export type TemplateInput = Omit<Template, 'id'>
