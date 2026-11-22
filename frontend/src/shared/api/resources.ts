import { z } from 'zod'
import { empty, json, request } from './client'
import { departmentSchema, profileSchema, suggestionSchema, templateSchema } from './contracts'
import type { DepartmentInput, SuggestionInput, TemplateInput } from './contracts'

export const api = {
  profile: (signal?: AbortSignal) => request('/Users/me', profileSchema, { signal }),
  login: (email: string, password: string) => request('/Users/login?useCookies=true', empty, json('POST', { email, password })),
  register: (email: string, password: string) => request('/Users/register', empty, json('POST', { email, password })),
  forgotPassword: (email: string) => request('/Users/forgotPassword', empty, json('POST', { email })),
  resetPassword: (email: string, resetCode: string, newPassword: string) => request('/Users/resetPassword', empty, json('POST', { email, resetCode, newPassword })),
  logout: () => request('/Users/logout', empty, json('POST', {})),
  suggestions: (signal?: AbortSignal) => request('/Suggestions/', z.array(suggestionSchema), { signal }),
  createSuggestion: (body: SuggestionInput) => request('/Suggestions/', z.string().uuid(), json('POST', body)),
  submitSuggestion: (id: string) => request(`/Suggestions/${id}/submit`, empty, json('POST')),
  approveSuggestion: (id: string, regNumber: string) => request(`/Suggestions/${id}/approve`, empty, json('POST', { regNumber })),
  returnSuggestion: (id: string, reason: string) => request(`/Suggestions/${id}/return`, empty, json('POST', { reason })),
  departments: (signal?: AbortSignal) => request('/Departments/', z.array(departmentSchema), { signal }),
  createDepartment: (body: DepartmentInput) => request('/Departments/', z.string().uuid(), json('POST', body)),
  updateDepartment: (id: string, body: DepartmentInput) => request(`/Departments/${id}`, z.string().uuid(), json('PUT', { id, ...body })),
  deleteDepartment: (id: string) => request(`/Departments/${id}`, empty, json('DELETE')),
  templates: (signal?: AbortSignal) => request('/Templates/', z.array(templateSchema), { signal }),
  createTemplate: (body: TemplateInput) => request('/Templates/', z.string().uuid(), json('POST', body)),
  updateTemplate: (id: string, body: TemplateInput) => request(`/Templates/${id}`, z.string().uuid(), json('PUT', { id, ...body })),
  archiveTemplate: (id: string) => request(`/Templates/${id}/archive`, empty, json('PATCH')),
}
