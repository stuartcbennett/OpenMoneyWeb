export interface Account {
  id: number
  name: string
  institution: string
}

export interface Investment {
  id: number
  name: string
  ticker?: string
  type: 'Stock' | 'MutualFund' | 'GIC'
  initialPrice: number
}

export interface Transaction {
  id: number
  accountId: number
  investmentId: number
  investmentName: string
  date: string
  activity: 'Buy' | 'Sell' | 'ReinvestDividend' | 'ReinvestInterest'
  quantity: number
  price: number
  total: number
  memo?: string
}

export interface ReturnResult {
  yearToDate?: number
  oneYear?: number
  threeYear?: number
  allTime?: number
}

export interface PortfolioInvestment {
  investment: Investment
  quantity: number
  currentPrice: number
  marketValue: number
  returns: ReturnResult
}

export interface PortfolioAccount {
  account: Account
  investments: PortfolioInvestment[]
  totalMarketValue: number
  returns: ReturnResult
}

export interface Portfolio {
  accounts: PortfolioAccount[]
  totalMarketValue: number
  returns: ReturnResult
}

export interface PriceHistoryEntry {
  id: number
  investmentId: number
  date: string
  price: number
}

export interface NetWorthPoint {
  date: string
  value: number
}

export interface InvestmentPerformance {
  investment: Investment
  quantity: number
  currentPrice: number
  marketValue: number
  returns: ReturnResult
}

export interface ImportResult {
  transactionCount: number
  errors: string[]
  newInvestments: Record<string, number>
}
