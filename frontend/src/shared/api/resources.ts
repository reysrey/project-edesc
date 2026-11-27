import { z } from 'zod'
import { empty, json, request } from './client'
import {
  departmentSchema,
  employeeDirectorySchema,
  serviceSettingsSchema,
  notificationSchema,
  profileSchema,
  suggestionDetailsSchema,
  suggestionSchema,
  suggestionSearchSchema,
  templateSchema,
  updateSuggestionResponseSchema,
} from './contracts'
import type {
  DepartmentInput,
  ServiceSettings,
  SuggestionInput,
  SuggestionSearch,
  TemplateInput,
  UpdateSuggestionInput,
} from './contracts'

export const api = {
  profile: (signal?: AbortSignal) => request('/Users/me', profileSchema, { signal }),
  employees: (params: URLSearchParams, signal?: AbortSignal) =>
    request(`/Users/directory?${params}`, employeeDirectorySchema, { signal }),
  notifications: (signal?: AbortSignal) =>
    request('/Users/notifications', z.array(notificationSchema), { signal }),
  serviceSettings: (signal?: AbortSignal) =>
    request('/ServiceSettings/', serviceSettingsSchema, { signal }),
  updateServiceSettings: (body: ServiceSettings) =>
    request('/ServiceSettings/', serviceSettingsSchema, json('PUT', body)),
  login: (email: string, password: string) =>
    request('/Users/login?useCookies=true', empty, json('POST', { email, password })),
  register: (email: string, password: string) =>
    request('/Users/register', empty, json('POST', { email, password })),
  forgotPassword: (email: string) =>
    request('/Users/forgotPassword', empty, json('POST', { email })),
  resetPassword: (email: string, resetCode: string, newPassword: string) =>
    request('/Users/resetPassword', empty, json('POST', { email, resetCode, newPassword })),
  logout: () => request('/Users/logout', empty, json('POST', {})),
  suggestions: (signal?: AbortSignal) =>
    request('/Suggestions/', z.array(suggestionSchema), { signal }),
  searchSuggestions: (
    { scope, search, status, sort, pageNumber, pageSize }: SuggestionSearch,
    signal?: AbortSignal,
  ) => {
    const params = new URLSearchParams({
      scope,
      sort,
      pageNumber: String(pageNumber),
      pageSize: String(pageSize),
    })
    if (search) params.set('search', search)
    if (status) params.set('status', status)
    return request(`/Suggestions/search?${params}`, suggestionSearchSchema, { signal })
  },
  suggestion: (id: string, signal?: AbortSignal) =>
    request(`/Suggestions/${id}`, suggestionDetailsSchema, { signal }),
  createSuggestion: (body: SuggestionInput, key: string) =>
    request('/Suggestions/', z.string().uuid(), {
      ...json('POST', body),
      headers: { 'Idempotency-Key': key },
    }),
  updateSuggestion: (id: string, body: UpdateSuggestionInput) =>
    request(`/Suggestions/${id}`, updateSuggestionResponseSchema, json('PUT', body)),
  submitSuggestion: (id: string) => request(`/Suggestions/${id}/submit`, empty, json('POST')),
  approveSuggestion: (id: string, regNumber: string) =>
    request(`/Suggestions/${id}/approve`, empty, json('POST', { regNumber })),
  returnSuggestion: (id: string, reason: string) =>
    request(`/Suggestions/${id}/return`, empty, json('POST', { reason })),
  commissionDecision: (
    id: string,
    decision: 'approve' | 'return' | 'reject',
    version: string,
    note: string,
  ) => request(`/Suggestions/${id}/commission/${decision}`, empty, json('POST', { version, note })),
  supportSuggestion: (id: string, supported: boolean) =>
    request(`/Suggestions/${id}/support`, empty, json(supported ? 'PUT' : 'DELETE')),
  departments: (signal?: AbortSignal) =>
    request('/Departments/', z.array(departmentSchema), { signal }),
  createDepartment: (body: DepartmentInput) =>
    request('/Departments/', z.string().uuid(), json('POST', body)),
  updateDepartment: (id: string, body: DepartmentInput) =>
    request(`/Departments/${id}`, z.string().uuid(), json('PUT', { id, ...body })),
  deleteDepartment: (id: string) => request(`/Departments/${id}`, empty, json('DELETE')),
  templates: (signal?: AbortSignal) => request('/Templates/', z.array(templateSchema), { signal }),
  createTemplate: (body: TemplateInput) =>
    request('/Templates/', z.string().uuid(), json('POST', body)),
  updateTemplate: (id: string, body: TemplateInput) =>
    request(`/Templates/${id}`, z.string().uuid(), json('PUT', { id, ...body })),
  archiveTemplate: (id: string) => request(`/Templates/${id}/archive`, empty, json('PATCH')),
}
