import { Routes, Route } from 'react-router-dom'
import Login from './pages/Login'
import Dashboard from './pages/Dashboard'
import LogsPage from './pages/LogsPage'
import RequireAuth from './components/RequireAuth'

function App() {
  return (
    <Routes>
      <Route path="/login" element={<Login />} />
      <Route
        path="/"
        element={
          <RequireAuth>
            <Dashboard />
          </RequireAuth>
        }
      />
      <Route
        path="/logs"
        element={
          <RequireAuth>
            <LogsPage />
          </RequireAuth>
        }
      />
    </Routes>
  )
}

export default App
