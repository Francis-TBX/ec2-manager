import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { listLogs } from '../api/logs'
import { listAccounts } from '../api/accounts'
import MultiSelectDropdown from '../components/MultiSelectDropdown'

const ACTION_TYPES = ['ManualStart', 'ManualStop', 'ScheduleStart', 'ScheduleStop']
const RESULTS = ['Success', 'Failed', 'Partial']
const PAGE_SIZE = 25

export default function LogsPage() {
  const [accountKeys, setAccountKeys] = useState<string[]>([])
  const [actionTypes, setActionTypes] = useState<string[]>([])
  const [results, setResults] = useState<string[]>([])
  const [instanceId, setInstanceId] = useState('')
  const [from, setFrom] = useState('')
  const [to, setTo] = useState('')
  const [page, setPage] = useState(1)

  const { data: accounts } = useQuery({ queryKey: ['accounts'], queryFn: listAccounts })

  const { data, isLoading, isError } = useQuery({
    queryKey: ['logs', accountKeys, actionTypes, results, instanceId, from, to, page],
    queryFn: () =>
      listLogs({
        accountKey: accountKeys,
        actionType: actionTypes,
        result: results,
        instanceId: instanceId || undefined,
        from: from ? new Date(from).toISOString() : undefined,
        to: to ? new Date(to).toISOString() : undefined,
        page,
        pageSize: PAGE_SIZE,
      }),
  })

  const totalPages = data ? Math.max(1, Math.ceil(data.totalCount / PAGE_SIZE)) : 1

  function withReset<T>(setter: (v: T) => void) {
    return (v: T) => {
      setter(v)
      setPage(1)
    }
  }

  const hasFilters = accountKeys.length > 0 || actionTypes.length > 0 || results.length > 0 || instanceId || from || to

  return (
    <div className="min-h-screen bg-slate-50">
      <header className="bg-gradient-to-r from-indigo-600 to-purple-600 text-white px-6 py-4 flex justify-between items-center shadow">
        <h1 className="text-xl font-bold">Audit Logs</h1>
        <Link to="/" className="text-sm bg-white/20 hover:bg-white/30 rounded-lg px-3 py-1.5 transition">
          ← Dashboard
        </Link>
      </header>

      <main className="p-6 space-y-4">
        {/* Filter bar */}
        <div className="bg-white rounded-xl shadow p-4 flex flex-wrap gap-4 items-start">
          <MultiSelectDropdown
            label="Account"
            options={(accounts ?? []).map((a) => ({ value: a.key, label: a.name }))}
            selected={accountKeys}
            onChange={withReset(setAccountKeys)}
          />

          <MultiSelectDropdown
            label="Action"
            options={ACTION_TYPES.map((a) => ({ value: a, label: a }))}
            selected={actionTypes}
            onChange={withReset(setActionTypes)}
          />

          <MultiSelectDropdown
            label="Result"
            options={RESULTS.map((r) => ({ value: r, label: r }))}
            selected={results}
            onChange={withReset(setResults)}
          />

          <div>
            <label className="block text-xs font-medium text-slate-500 mb-1">Instance ID</label>
            <input
              type="text"
              placeholder="i-…"
              value={instanceId}
              onChange={(e) => withReset(setInstanceId)(e.target.value)}
              className="rounded-lg border border-slate-300 px-3 py-1.5 text-sm w-40"
            />
          </div>

          <div>
            <label className="block text-xs font-medium text-slate-500 mb-1">From</label>
            <input
              type="datetime-local"
              value={from}
              onChange={(e) => withReset(setFrom)(e.target.value)}
              className="rounded-lg border border-slate-300 px-3 py-1.5 text-sm"
            />
          </div>

          <div>
            <label className="block text-xs font-medium text-slate-500 mb-1">To</label>
            <input
              type="datetime-local"
              value={to}
              onChange={(e) => withReset(setTo)(e.target.value)}
              className="rounded-lg border border-slate-300 px-3 py-1.5 text-sm"
            />
          </div>

          {hasFilters && (
            <button
              onClick={() => {
                setAccountKeys([])
                setActionTypes([])
                setResults([])
                setInstanceId('')
                setFrom('')
                setTo('')
                setPage(1)
              }}
              className="text-sm bg-white border border-slate-300 rounded-lg px-3 py-1.5 self-end"
            >
              Clear filters
            </button>
          )}
        </div>

        {/* Table */}
        <div className="bg-white rounded-xl shadow overflow-hidden">
          {isLoading ? (
            <div className="p-8 text-center text-slate-400">Loading logs…</div>
          ) : isError ? (
            <div className="p-8 text-center text-red-500">Failed to load logs.</div>
          ) : (data?.items.length ?? 0) === 0 ? (
            <div className="p-8 text-center text-slate-400">No logs found.</div>
          ) : (
            <table className="w-full text-sm">
              <thead className="bg-slate-50 border-b border-slate-200">
                <tr>
                  <th className="px-4 py-2 text-left font-medium text-slate-500">Timestamp</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500">User</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500">Action</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500">Account / Region</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500">Instances</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500">Dry Run</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500">Result</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500">Message</th>
                </tr>
              </thead>
              <tbody>
                {data!.items.map((l, idx) => (
                  <tr
                    key={l.id}
                    className={`border-b border-slate-100 ${idx % 2 === 0 ? 'bg-white' : 'bg-slate-50/50'}`}
                  >
                    <td className="px-4 py-2 text-slate-600 whitespace-nowrap">{new Date(l.timestamp).toLocaleString()}</td>
                    <td className="px-4 py-2 text-slate-600">{l.userName ?? '—'}</td>
                    <td className="px-4 py-2 font-medium text-slate-800">{l.actionType}</td>
                    <td className="px-4 py-2 text-slate-600">{l.accountKey} / {l.region}</td>
                    <td className="px-4 py-2 text-slate-500 text-xs">{l.instanceIds.join(', ')}</td>
                    <td className="px-4 py-2">
                      {l.dryRun && <span className="text-xs bg-slate-100 text-slate-500 rounded-full px-2 py-0.5">dry run</span>}
                    </td>
                    <td className="px-4 py-2">
                      <span
                        className={`text-xs px-2 py-0.5 rounded-full ${
                          l.result === 'Success'
                            ? 'bg-teal-100 text-teal-800'
                            : l.result === 'Failed'
                            ? 'bg-red-100 text-red-700'
                            : 'bg-amber-100 text-amber-800'
                        }`}
                      >
                        {l.result}
                      </span>
                    </td>
                    <td className="px-4 py-2 text-slate-500 text-xs max-w-xs truncate" title={l.message}>{l.message}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>

        {/* Pagination */}
        {data && data.totalCount > 0 && (
          <div className="flex justify-between items-center text-sm text-slate-500">
            <span>
              Page {data.page} of {totalPages} — {data.totalCount} total
            </span>
            <div className="flex gap-2">
              <button
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                disabled={page <= 1}
                className="bg-white border border-slate-300 rounded-lg px-3 py-1.5 disabled:opacity-50"
              >
                Previous
              </button>
              <button
                onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                disabled={page >= totalPages}
                className="bg-white border border-slate-300 rounded-lg px-3 py-1.5 disabled:opacity-50"
              >
                Next
              </button>
            </div>
          </div>
        )}
      </main>
    </div>
  )
}
