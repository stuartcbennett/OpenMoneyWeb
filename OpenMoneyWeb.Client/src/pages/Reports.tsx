import { useEffect, useState } from 'react'
import {
  Box, Typography, Tabs, Tab, Select, MenuItem, FormControl,
  InputLabel, CircularProgress, Table, TableHead, TableRow, TableCell, TableBody, Chip
} from '@mui/material'
import { LineChart, Line, XAxis, YAxis, Tooltip, ResponsiveContainer, CartesianGrid } from 'recharts'
import { api } from '../api/client'
import type { Account, Investment, NetWorthPoint, InvestmentPerformance, PriceHistoryEntry } from '../types'

const currency = (v: number) =>
  v.toLocaleString('en-CA', { style: 'currency', currency: 'CAD' })

const pct = (v?: number) => v == null ? '—' : `${(v * 100).toFixed(2)}%`

function NetWorthChart({ data }: { data: NetWorthPoint[] }) {
  if (!data.length) return <Typography color="text.secondary">No data</Typography>
  const chartData = data.map(p => ({ date: new Date(p.date).toLocaleDateString(), value: p.value }))
  return (
    <ResponsiveContainer width="100%" height={300}>
      <LineChart data={chartData}>
        <CartesianGrid strokeDasharray="3 3" stroke="#444" />
        <XAxis dataKey="date" tick={{ fontSize: 11 }} />
        <YAxis tickFormatter={v => `$${(v / 1000).toFixed(0)}k`} tick={{ fontSize: 11 }} />
        <Tooltip formatter={(v: number) => currency(v)} />
        <Line type="monotone" dataKey="value" stroke="#00acc1" dot={false} strokeWidth={2} />
      </LineChart>
    </ResponsiveContainer>
  )
}

export default function ReportsPage() {
  const [tab, setTab] = useState(0)
  const [accounts, setAccounts] = useState<Account[]>([])
  const [investments, setInvestments] = useState<Investment[]>([])
  const [netWorth, setNetWorth] = useState<NetWorthPoint[]>([])
  const [accountValue, setAccountValue] = useState<NetWorthPoint[]>([])
  const [priceHistory, setPriceHistory] = useState<PriceHistoryEntry[]>([])
  const [performance, setPerformance] = useState<InvestmentPerformance[]>([])
  const [selectedAccount, setSelectedAccount] = useState<number | ''>('')
  const [selectedInvestment, setSelectedInvestment] = useState<number | ''>('')
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    api.accounts.list().then(setAccounts)
    api.investments.list().then(setInvestments)
    api.reports.netWorth().then(setNetWorth)
    api.reports.investmentPerformance().then(setPerformance)
  }, [])

  useEffect(() => {
    if (!selectedAccount) return
    setLoading(true)
    api.reports.accountValue(selectedAccount)
      .then(setAccountValue)
      .finally(() => setLoading(false))
  }, [selectedAccount])

  useEffect(() => {
    if (!selectedInvestment) return
    api.reports.investmentPriceHistory(selectedInvestment).then(setPriceHistory)
  }, [selectedInvestment])

  return (
    <Box sx={{ p: 3, maxWidth: 1100, mx: 'auto' }}>
      <Typography variant="h4" sx={{ mb: 2 }}>Reports</Typography>

      <Tabs value={tab} onChange={(_, v) => setTab(v)} sx={{ mb: 3 }}>
        <Tab label="Net Worth" />
        <Tab label="Account Value" />
        <Tab label="Price History" />
        <Tab label="Performance" />
      </Tabs>

      {tab === 0 && (
        <Box>
          <Typography variant="h6" sx={{ mb: 2 }}>Net Worth Over Time</Typography>
          <NetWorthChart data={netWorth} />
        </Box>
      )}

      {tab === 1 && (
        <Box>
          <FormControl sx={{ minWidth: 240, mb: 3 }}>
            <InputLabel>Account</InputLabel>
            <Select value={selectedAccount} label="Account" onChange={e => setSelectedAccount(e.target.value as number)}>
              {accounts.map(a => <MenuItem key={a.id} value={a.id}>{a.name}</MenuItem>)}
            </Select>
          </FormControl>
          {loading ? <CircularProgress /> : <NetWorthChart data={accountValue} />}
        </Box>
      )}

      {tab === 2 && (
        <Box>
          <FormControl sx={{ minWidth: 240, mb: 3 }}>
            <InputLabel>Investment</InputLabel>
            <Select value={selectedInvestment} label="Investment" onChange={e => setSelectedInvestment(e.target.value as number)}>
              {investments.map(i => <MenuItem key={i.id} value={i.id}>{i.name}</MenuItem>)}
            </Select>
          </FormControl>
          {priceHistory.length > 0 && (
            <ResponsiveContainer width="100%" height={300}>
              <LineChart data={priceHistory.map(p => ({ date: new Date(p.date).toLocaleDateString(), price: p.price }))}>
                <CartesianGrid strokeDasharray="3 3" stroke="#444" />
                <XAxis dataKey="date" tick={{ fontSize: 11 }} />
                <YAxis tickFormatter={v => `$${v.toFixed(2)}`} tick={{ fontSize: 11 }} />
                <Tooltip formatter={(v: number) => currency(v)} />
                <Line type="monotone" dataKey="price" stroke="#c6ff00" dot={false} strokeWidth={2} />
              </LineChart>
            </ResponsiveContainer>
          )}
        </Box>
      )}

      {tab === 3 && (
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Investment</TableCell>
              <TableCell>Type</TableCell>
              <TableCell align="right">Qty</TableCell>
              <TableCell align="right">Price</TableCell>
              <TableCell align="right">Market Value</TableCell>
              <TableCell>YTD</TableCell>
              <TableCell>1 yr</TableCell>
              <TableCell>3 yr</TableCell>
              <TableCell>All time</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {performance.map(p => (
              <TableRow key={p.investment.id} hover>
                <TableCell>{p.investment.name}</TableCell>
                <TableCell>{p.investment.type}</TableCell>
                <TableCell align="right">{p.quantity.toFixed(4)}</TableCell>
                <TableCell align="right">{currency(p.currentPrice)}</TableCell>
                <TableCell align="right">{currency(p.marketValue)}</TableCell>
                {[p.returns.yearToDate, p.returns.oneYear, p.returns.threeYear, p.returns.allTime].map((v, i) => (
                  <TableCell key={i}>
                    <Chip
                      label={pct(v)}
                      size="small"
                      color={(v ?? 0) > 0 ? 'success' : (v ?? 0) < 0 ? 'error' : 'default'}
                      variant="outlined"
                    />
                  </TableCell>
                ))}
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}
    </Box>
  )
}
