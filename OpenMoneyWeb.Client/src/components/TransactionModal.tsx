import { useEffect, useState } from 'react'
import {
  Dialog, DialogTitle, DialogContent, DialogActions,
  Button, TextField, Select, MenuItem, FormControl, InputLabel, Box
} from '@mui/material'
import { api } from '../api/client'
import type { Transaction, Investment } from '../types'

interface Props {
  open: boolean
  tx?: Transaction
  accountId: number
  onClose: () => void
  onSaved: () => void
}

const ACTIVITIES: Transaction['activity'][] = ['Buy', 'Sell', 'ReinvestDividend', 'ReinvestInterest']

export default function TransactionModal({ open, tx, accountId, onClose, onSaved }: Props) {
  const [investments, setInvestments] = useState<Investment[]>([])
  const [investmentId, setInvestmentId] = useState<number | ''>('')
  const [date, setDate] = useState('')
  const [activity, setActivity] = useState<Transaction['activity']>('Buy')
  const [quantity, setQuantity] = useState('')
  const [price, setPrice] = useState('')
  const [total, setTotal] = useState('')
  const [memo, setMemo] = useState('')

  useEffect(() => {
    api.investments.list().then(setInvestments)
  }, [])

  useEffect(() => {
    if (tx) {
      setInvestmentId(tx.investmentId)
      setDate(tx.date.slice(0, 10))
      setActivity(tx.activity)
      setQuantity(String(tx.quantity))
      setPrice(String(tx.price))
      setTotal(String(tx.total))
      setMemo(tx.memo ?? '')
    } else {
      setInvestmentId(''); setDate(''); setActivity('Buy')
      setQuantity(''); setPrice(''); setTotal(''); setMemo('')
    }
  }, [tx, open])

  const handleSave = async () => {
    const data = {
      accountId,
      investmentId: investmentId as number,
      date: new Date(date).toISOString(),
      activity,
      quantity: parseFloat(quantity),
      price: parseFloat(price),
      total: parseFloat(total),
      memo: memo || undefined
    }
    if (tx) await api.transactions.update(tx.id, data)
    else await api.transactions.create(data)
    onSaved()
    onClose()
  }

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>{tx ? 'Edit Transaction' : 'Add Transaction'}</DialogTitle>
      <DialogContent>
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 1 }}>
          <TextField label="Date" type="date" value={date} onChange={e => setDate(e.target.value)} InputLabelProps={{ shrink: true }} fullWidth />
          <FormControl fullWidth>
            <InputLabel>Investment</InputLabel>
            <Select value={investmentId} label="Investment" onChange={e => setInvestmentId(e.target.value as number)}>
              {investments.map(i => <MenuItem key={i.id} value={i.id}>{i.name}</MenuItem>)}
            </Select>
          </FormControl>
          <FormControl fullWidth>
            <InputLabel>Activity</InputLabel>
            <Select value={activity} label="Activity" onChange={e => setActivity(e.target.value as Transaction['activity'])}>
              {ACTIVITIES.map(a => <MenuItem key={a} value={a}>{a}</MenuItem>)}
            </Select>
          </FormControl>
          <Box sx={{ display: 'flex', gap: 2 }}>
            <TextField label="Quantity" type="number" value={quantity} onChange={e => setQuantity(e.target.value)} fullWidth />
            <TextField label="Price" type="number" value={price} onChange={e => setPrice(e.target.value)} fullWidth />
            <TextField label="Total" type="number" value={total} onChange={e => setTotal(e.target.value)} fullWidth />
          </Box>
          <TextField label="Memo (optional)" value={memo} onChange={e => setMemo(e.target.value)} fullWidth />
        </Box>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button variant="contained" onClick={handleSave}>Save</Button>
      </DialogActions>
    </Dialog>
  )
}
