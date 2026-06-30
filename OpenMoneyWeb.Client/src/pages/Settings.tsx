import { useEffect, useState } from 'react'
import {
  Box, Typography, Tabs, Tab, Table, TableHead, TableRow, TableCell, TableBody,
  IconButton, Button, TextField, Dialog, DialogTitle, DialogContent,
  DialogActions, Select, MenuItem, FormControl, InputLabel, Alert
} from '@mui/material'
import DeleteIcon from '@mui/icons-material/Delete'
import EditIcon from '@mui/icons-material/Edit'
import AddIcon from '@mui/icons-material/Add'
import { api } from '../api/client'
import type { Account, Investment, ImportResult } from '../types'

const currency = (v: number) =>
  v.toLocaleString('en-CA', { style: 'currency', currency: 'CAD' })

function AccountsTab() {
  const [accounts, setAccounts] = useState<Account[]>([])
  const [open, setOpen] = useState(false)
  const [editing, setEditing] = useState<Account | null>(null)
  const [name, setName] = useState('')
  const [institution, setInstitution] = useState('')

  const load = () => api.accounts.list().then(setAccounts)
  useEffect(() => { load() }, [])

  const openAdd = () => { setEditing(null); setName(''); setInstitution(''); setOpen(true) }
  const openEdit = (a: Account) => { setEditing(a); setName(a.name); setInstitution(a.institution); setOpen(true) }

  const save = async () => {
    if (editing) await api.accounts.update(editing.id, { name, institution })
    else await api.accounts.create({ name, institution })
    setOpen(false)
    load()
  }

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'flex-end', mb: 1 }}>
        <Button startIcon={<AddIcon />} variant="contained" onClick={openAdd}>Add Account</Button>
      </Box>
      <Table size="small">
        <TableHead><TableRow><TableCell>Name</TableCell><TableCell>Institution</TableCell><TableCell /></TableRow></TableHead>
        <TableBody>
          {accounts.map(a => (
            <TableRow key={a.id} hover>
              <TableCell>{a.name}</TableCell>
              <TableCell>{a.institution}</TableCell>
              <TableCell>
                <IconButton size="small" onClick={() => openEdit(a)}><EditIcon fontSize="small" /></IconButton>
                <IconButton size="small" onClick={() => api.accounts.delete(a.id).then(load)}><DeleteIcon fontSize="small" /></IconButton>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>

      <Dialog open={open} onClose={() => setOpen(false)} maxWidth="xs" fullWidth>
        <DialogTitle>{editing ? 'Edit Account' : 'Add Account'}</DialogTitle>
        <DialogContent sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 2 }}>
          <TextField label="Name" value={name} onChange={e => setName(e.target.value)} fullWidth />
          <TextField label="Institution" value={institution} onChange={e => setInstitution(e.target.value)} fullWidth />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpen(false)}>Cancel</Button>
          <Button variant="contained" onClick={save}>Save</Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}

function InvestmentsTab() {
  const [investments, setInvestments] = useState<Investment[]>([])
  const [open, setOpen] = useState(false)
  const [editing, setEditing] = useState<Investment | null>(null)
  const [name, setName] = useState('')
  const [ticker, setTicker] = useState('')
  const [type, setType] = useState('Stock')
  const [initialPrice, setInitialPrice] = useState('')

  const load = () => api.investments.list().then(setInvestments)
  useEffect(() => { load() }, [])

  const openAdd = () => { setEditing(null); setName(''); setTicker(''); setType('Stock'); setInitialPrice(''); setOpen(true) }
  const openEdit = (i: Investment) => {
    setEditing(i); setName(i.name); setTicker(i.ticker ?? ''); setType(i.type); setInitialPrice(String(i.initialPrice)); setOpen(true)
  }

  const save = async () => {
    const data = { name, ticker: ticker || undefined, type, initialPrice: parseFloat(initialPrice) }
    if (editing) await api.investments.update(editing.id, data)
    else await api.investments.create(data)
    setOpen(false)
    load()
  }

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'flex-end', mb: 1 }}>
        <Button startIcon={<AddIcon />} variant="contained" onClick={openAdd}>Add Investment</Button>
      </Box>
      <Table size="small">
        <TableHead><TableRow><TableCell>Name</TableCell><TableCell>Ticker</TableCell><TableCell>Type</TableCell><TableCell align="right">Initial Price</TableCell><TableCell /></TableRow></TableHead>
        <TableBody>
          {investments.map(i => (
            <TableRow key={i.id} hover>
              <TableCell>{i.name}</TableCell>
              <TableCell>{i.ticker}</TableCell>
              <TableCell>{i.type}</TableCell>
              <TableCell align="right">{currency(i.initialPrice)}</TableCell>
              <TableCell>
                <IconButton size="small" onClick={() => openEdit(i)}><EditIcon fontSize="small" /></IconButton>
                <IconButton size="small" onClick={() => api.investments.delete(i.id).then(load)}><DeleteIcon fontSize="small" /></IconButton>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>

      <Dialog open={open} onClose={() => setOpen(false)} maxWidth="xs" fullWidth>
        <DialogTitle>{editing ? 'Edit Investment' : 'Add Investment'}</DialogTitle>
        <DialogContent sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 2 }}>
          <TextField label="Name" value={name} onChange={e => setName(e.target.value)} fullWidth />
          <TextField label="Ticker (optional)" value={ticker} onChange={e => setTicker(e.target.value)} fullWidth />
          <FormControl fullWidth>
            <InputLabel>Type</InputLabel>
            <Select value={type} label="Type" onChange={e => setType(e.target.value)}>
              <MenuItem value="Stock">Stock</MenuItem>
              <MenuItem value="MutualFund">Mutual Fund</MenuItem>
              <MenuItem value="GIC">GIC</MenuItem>
            </Select>
          </FormControl>
          <TextField label="Initial Price" type="number" value={initialPrice} onChange={e => setInitialPrice(e.target.value)} fullWidth />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpen(false)}>Cancel</Button>
          <Button variant="contained" onClick={save}>Save</Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}

function ImportTab() {
  const [text, setText] = useState('')
  const [result, setResult] = useState<ImportResult | null>(null)
  const [error, setError] = useState<string | null>(null)

  const handleImport = async () => {
    setResult(null); setError(null)
    try {
      const r = await api.import.importText(text)
      setResult(r)
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Import failed')
    }
  }

  return (
    <Box>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        Paste transaction data in the legacy CSV format (see InvestmentTransactionsSample.txt for the format).
      </Typography>
      <TextField
        multiline rows={12} fullWidth
        value={text} onChange={e => setText(e.target.value)}
        placeholder="Paste transaction text here..."
        sx={{ mb: 2, fontFamily: 'monospace' }}
      />
      <Button variant="contained" onClick={handleImport} disabled={!text.trim()}>
        Import
      </Button>

      {result && (
        <Box sx={{ mt: 2 }}>
          <Alert severity={result.errors.length ? 'warning' : 'success'}>
            Imported {result.transactionCount} transaction(s).
            {result.errors.length > 0 && ` ${result.errors.length} error(s).`}
          </Alert>
          {Object.keys(result.newInvestments).length > 0 && (
            <Box sx={{ mt: 1 }}>
              <Typography variant="body2" color="warning.main">
                Unknown investments (create them in the Investments tab first):
              </Typography>
              {Object.entries(result.newInvestments).map(([name, price]) => (
                <Typography key={name} variant="body2">• {name} (first price: {currency(price)})</Typography>
              ))}
            </Box>
          )}
          {result.errors.map((e, i) => (
            <Typography key={i} variant="body2" color="error">{e}</Typography>
          ))}
        </Box>
      )}
      {error && <Alert severity="error" sx={{ mt: 2 }}>{error}</Alert>}
    </Box>
  )
}

export default function SettingsPage() {
  const [tab, setTab] = useState(0)

  return (
    <Box sx={{ p: 3, maxWidth: 1100, mx: 'auto' }}>
      <Typography variant="h4" sx={{ mb: 2 }}>Settings</Typography>
      <Tabs value={tab} onChange={(_, v) => setTab(v)} sx={{ mb: 3 }}>
        <Tab label="Accounts" />
        <Tab label="Investments" />
        <Tab label="Import" />
      </Tabs>
      {tab === 0 && <AccountsTab />}
      {tab === 1 && <InvestmentsTab />}
      {tab === 2 && <ImportTab />}
    </Box>
  )
}
