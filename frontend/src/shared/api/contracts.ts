import { z } from 'zod'

export const statuses = ['Draft', 'Submitted', 'ReturnedByManager', 'InCommission', 'RevisionRequired', 'Rejected', 'ApprovedPlanning', 'InImplementation', 'Implemented'] as const
export const suggestionSchema = z.object({
  id: z.string().uuid(), title: z.string(), status: z.enum(statuses), created: z.string(),
})
export const departmentSchema = z.object({
  id: z.string().uuid(), name: z.string(), headUserId: z.string().nullable(), employeesCount: z.number().int().nonnegative(),
})
export const templateSchema = z.object({
  id: z.string().uuid(), type: z.enum(['Template', 'Bottleneck']), departmentId: z.string().uuid().nullable(),
  improvementObject: z.string(), title: z.string(), problemHint: z.string(), solutionHint: z.string(), isActive: z.boolean(),
})
export const profileSchema = z.object({
  id: z.string(), email: z.string(), fullName: z.string(), position: z.string(),
  departmentId: z.string().uuid().nullable(), roles: z.array(z.string()),
})
export type Suggestion = z.infer<typeof suggestionSchema>
export type Department = z.infer<typeof departmentSchema>
export type Template = z.infer<typeof templateSchema>
export type Profile = z.infer<typeof profileSchema>
export type SuggestionInput = { title: string; currentFlaws: string; proposedSolution: string; improvementObject: string; targetDepartmentId: string }
export type DepartmentInput = Omit<Department, 'id'>
export type TemplateInput = Omit<Template, 'id'>
