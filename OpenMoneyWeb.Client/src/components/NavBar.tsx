import { AppBar, Toolbar, Typography, Button, Box } from '@mui/material'
import { Link, useLocation } from 'react-router-dom'

const navItems = [
  { label: 'Portfolio', path: '/portfolio' },
  { label: 'Accounts', path: '/accounts' },
  { label: 'Reports', path: '/reports' },
  { label: 'Settings', path: '/settings' }
]

export default function NavBar() {
  const { pathname } = useLocation()
  return (
    <AppBar position="static">
      <Toolbar>
        <Typography variant="h6" sx={{ mr: 4, fontWeight: 700 }}>
          OpenMoney
        </Typography>
        <Box sx={{ display: 'flex', gap: 1 }}>
          {navItems.map(({ label, path }) => (
            <Button
              key={path}
              component={Link}
              to={path}
              color={pathname === path ? 'secondary' : 'inherit'}
              variant={pathname === path ? 'outlined' : 'text'}
            >
              {label}
            </Button>
          ))}
        </Box>
      </Toolbar>
    </AppBar>
  )
}
