import { useEffect, useState } from 'react'
import {
  Box, Typography, CircularProgress, Accordion, AccordionSummary,
  AccordionDetails, Table, TableHead, TableRow, TableCell, TableBody, Chip
} from '@mui/material'
import ExpandMoreIcon from '@mui/icons-material/ExpandMore'
import { api } from '../api/client'
import type { Portfolio, ReturnResult } from '../types'

const pct = (v?: number) =>
  v == null ? '—' : `${(v * 100).toFixed(2)}%`

const currency = (v: number) =>
  v.toLocaleString('en-CA', { style: 'currency', currency: 'CAD' })

function ReturnChips({ r }: { r: ReturnResult }) {
  return (
    <Box sx={{ display: 'flex', gap: 0.5, flexWrap: 'wrap' }}>
      {[['YTD', r.yearToDate], ['1yr', r.oneYear], ['3yr', r.threeYear], ['All', r.allTime]].map(
        ([label, val]) => (
          <Chip
            key={label as string}
            label={`${label} ${pct(val as number | undefined)}`}
            size="small"
            color={(val as number) > 0 ? 'success' : (val as number) < 0 ? 'error' : 'default'}
            variant="outlined"
          />
        )
      )}
    </Box>
  )
}

export default function PortfolioPage() {
  const [portfolio, setPortfolio] = useState<Portfolio | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    api.portfolio.get()
      .then(setPortfolio)
      .catch(e => setError(e.message))
      .finally(() => setLoading(false))
  }, [])

  if (loading) return <Box sx={{ p: 4, textAlign: 'center' }}><CircularProgress /></Box>
  if (error) return <Box sx={{ p: 4 }}><Typography color="error">{error}</Typography></Box>
  if (!portfolio) return null

  return (
    <Box sx={{ p: 3, maxWidth: 1100, mx: 'auto' }}>
      <Box sx={{ mb: 3 }}>
        <Typography variant="h4">Portfolio</Typography>
        <Typography variant="h5" color="primary">{currency(portfolio.totalMarketValue)}</Typography>
        <ReturnChips r={portfolio.returns} />
      </Box>

      {portfolio.accounts.map(pa => (
        <Accordion key={pa.account.id} defaultExpanded>
          <AccordionSummary expandIcon={<ExpandMoreIcon />}>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 3, width: '100%', pr: 2 }}>
              <Typography sx={{ flexGrow: 1, fontWeight: 600 }}>
                {pa.account.name}
                <Typography component="span" variant="body2" color="text.secondary" sx={{ ml: 1 }}>
                  {pa.account.institution}
                </Typography>
              </Typography>
              <Typography>{currency(pa.totalMarketValue)}</Typography>
              <ReturnChips r={pa.returns} />
            </Box>
          </AccordionSummary>
          <AccordionDetails sx={{ p: 0 }}>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Investment</TableCell>
                  <TableCell align="right">Qty</TableCell>
                  <TableCell align="right">Price</TableCell>
                  <TableCell align="right">Market Value</TableCell>
                  <TableCell>Returns</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {pa.investments.map(pi => (
                  <TableRow key={pi.investment.id} hover>
                    <TableCell>
                      {pi.investment.name}
                      {pi.investment.ticker && (
                        <Typography component="span" variant="caption" color="text.secondary" sx={{ ml: 1 }}>
                          {pi.investment.ticker}
                        </Typography>
                      )}
                    </TableCell>
                    <TableCell align="right">{pi.quantity.toFixed(4)}</TableCell>
                    <TableCell align="right">{currency(pi.currentPrice)}</TableCell>
                    <TableCell align="right">{currency(pi.marketValue)}</TableCell>
                    <TableCell><ReturnChips r={pi.returns} /></TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </AccordionDetails>
        </Accordion>
      ))}
    </Box>
  )
}
