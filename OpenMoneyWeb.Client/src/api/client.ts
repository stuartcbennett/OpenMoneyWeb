import type {
  Account, Investment, Transaction, Portfolio,
  PriceHistoryEntry, NetWorthPoint, InvestmentPerformance, ImportResult
} from '../types'

const BASE = '/api'

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`${BASE}${path}`, init)
  if (!res.ok) throw new Error(await res.text())
  if (res.status === 204) return undefined as T
  return res.json() as Promise<T>
}

const json = (body: unknown) => ({
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify(body)
})

export const api = {
  portfolio: {
    get: () => request<Portfolio>('/portfolio')
  },

  accounts: {
    list: () => request<Account[]>('/accounts'),
    create: (data: { name: string; institution: string }) =>
      request<Account>('/accounts', { method: 'POST', ...json(data) }),
    update: (id: number, data: { name: string; institution: string }) =>
      request<void>(`/accounts/${id}`, { method: 'PUT', ...json(data) }),
    delete: (id: number) =>
      request<void>(`/accounts/${id}`, { method: 'DELETE' }),
    transactions: (accountId: number) =>
      request<Transaction[]>(`/accounts/${accountId}/transactions`)
  },

  investments: {
    list: () => request<Investment[]>('/investments'),
    create: (data: { name: string; ticker?: string; type: string; initialPrice: number }) =>
      request<Investment>('/investments', { method: 'POST', ...json(data) }),
    update: (id: number, data: { name: string; ticker?: string; type: string; initialPrice: number }) =>
      request<void>(`/investments/${id}`, { method: 'PUT', ...json(data) }),
    delete: (id: number) =>
      request<void>(`/investments/${id}`, { method: 'DELETE' }),
    addPrice: (id: number, data: { date: string; price: number }) =>
      request<void>(`/investments/${id}/price`, { method: 'POST', ...json(data) })
  },

  transactions: {
    create: (data: Omit<Transaction, 'id' | 'investmentName'>) =>
      request<Transaction>('/transactions', { method: 'POST', ...json(data) }),
    update: (id: number, data: Omit<Transaction, 'id' | 'investmentName'>) =>
      request<void>(`/transactions/${id}`, { method: 'PUT', ...json(data) }),
    delete: (id: number) =>
      request<void>(`/transactions/${id}`, { method: 'DELETE' })
  },

  reports: {
    netWorth: () => request<NetWorthPoint[]>('/reports/networth'),
    accountValue: (accountId: number) =>
      request<NetWorthPoint[]>(`/reports/account/${accountId}/value`),
    investmentPriceHistory: (investmentId: number) =>
      request<PriceHistoryEntry[]>(`/reports/investments/${investmentId}/pricehistory`),
    investmentPerformance: () =>
      request<InvestmentPerformance[]>('/reports/investments/performance')
  },

  import: {
    importText: (text: string) =>
      request<ImportResult>('/import', { method: 'POST', ...json({ text }) })
  }
}
