import { useEffect, useState } from 'react'
import {
  Box, Typography, Select, MenuItem, FormControl, InputLabel,
  Table, TableHead, TableRow, TableCell, TableBody, IconButton,
  CircularProgress, Button
} from '@mui/material'
import DeleteIcon from '@mui/icons-material/Delete'
import EditIcon from '@mui/icons-material/Edit'
import AddIcon from '@mui/icons-material/Add'
import { api } from '../api/client'
import type { Account, Transaction } from '../types'
import TransactionModal from '../components/TransactionModal'

const currency = (v: number) =>
  v.toLocaleString('en-CA', { style: 'currency', currency: 'CAD' })

export default function AccountsPage() {
  const [accounts, setAccounts] = useState<Account[]>([])
  const [selectedId, setSelectedId] = useState<number | ''>('')
  const [transactions, setTransactions] = useState<Transaction[]>([])
  const [loading, setLoading] = useState(false)
  const [modalOpen, setModalOpen] = useState(false)
  const [editTx, setEditTx] = useState<Transaction | undefined>()

  useEffect(() => {
    api.accounts.list().then(setAccounts)
  }, [])

  useEffect(() => {
    if (!selectedId) return
    setLoading(true)
    api.accounts.transactions(selectedId)
      .then(setTransactions)
      .finally(() => setLoading(false))
  }, [selectedId])

  const refresh = () => {
    if (selectedId) api.accounts.transactions(selectedId).then(setTransactions)
  }

  const handleDelete = async (id: number) => {
    await api.transactions.delete(id)
    refresh()
  }

  const openAdd = () => { setEditTx(undefined); setModalOpen(true) }
  const openEdit = (tx: Transaction) => { setEditTx(tx); setModalOpen(true) }

  return (
    <Box sx={{ p: 3, maxWidth: 1100, mx: 'auto' }}>
      <Typography variant="h4" sx={{ mb: 2 }}>Accounts</Typography>

      <FormControl sx={{ minWidth: 240, mb: 3 }}>
        <InputLabel>Account</InputLabel>
        <Select
          value={selectedId}
          label="Account"
          onChange={e => setSelectedId(e.target.value as number)}
        >
          {accounts.map(a => (
            <MenuItem key={a.id} value={a.id}>{a.name} — {a.institution}</MenuItem>
          ))}
        </Select>
      </FormControl>

      {selectedId && (
        <>
          <Box sx={{ display: 'flex', justifyContent: 'flex-end', mb: 1 }}>
            <Button startIcon={<AddIcon />} variant="contained" onClick={openAdd}>
              Add Transaction
            </Button>
          </Box>

          {loading
            ? <CircularProgress />
            : (
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Date</TableCell>
                    <TableCell>Investment</TableCell>
                    <TableCell>Activity</TableCell>
                    <TableCell align="right">Qty</TableCell>
                    <TableCell align="right">Price</TableCell>
                    <TableCell align="right">Total</TableCell>
                    <TableCell>Memo</TableCell>
                    <TableCell />
                  </TableRow>
                </TableHead>
                <TableBody>
                  {transactions.map(tx => (
                    <TableRow key={tx.id} hover>
                      <TableCell>{new Date(tx.date).toLocaleDateString()}</TableCell>
                      <TableCell>{tx.investmentName}</TableCell>
                      <TableCell>{tx.activity}</TableCell>
                      <TableCell align="right">{tx.quantity.toFixed(4)}</TableCell>
                      <TableCell align="right">{currency(tx.price)}</TableCell>
                      <TableCell align="right">{currency(tx.total)}</TableCell>
                      <TableCell>{tx.memo}</TableCell>
                      <TableCell>
                        <IconButton size="small" onClick={() => openEdit(tx)}><EditIcon fontSize="small" /></IconButton>
                        <IconButton size="small" onClick={() => handleDelete(tx.id)}><DeleteIcon fontSize="small" /></IconButton>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )
          }
        </>
      )}

      <TransactionModal
        open={modalOpen}
        tx={editTx}
        accountId={selectedId as number}
        onClose={() => setModalOpen(false)}
        onSaved={refresh}
      />
    </Box>
  )
}
