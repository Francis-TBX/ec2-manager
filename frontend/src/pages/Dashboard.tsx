import { useState, useMemo } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { useAuthStore } from '../store/authStore'
import { listInstances, startInstances, stopInstances, type Instance } from '../api/instances'
import { listAccounts } from '../api/accounts'
import StatusBadge from '../components/StatusBadge'
import InstanceDetailsModal from '../components/InstanceDetailsModal'

const STATUS_OPTIONS = ['pending', 'running', 'shutting-down', 'terminated', 'stopping', 'stopped']

export default function Dashboard() {
  const { username, logout } = useAuthStore()
  const queryClient = useQueryClient()

  const [accountKey, setAccountKey] = useState('AWS')
  const [region, setRegion] = useState('')
  const [statuses, setStatuses] = useState<string[]>([])
  const [search, setSearch] = useState('')
  const [hideProtected, setHideProtected] = useState(false)
  const [selected, setSelected] = useState<Set<string>>(new Set())
  const [actionMessage, setActionMessage] = useState<string | null>(null)
  const [detailsInstanceId, setDetailsInstanceId] = useState<string | null>(null)

  const { data: accounts } = useQuery({ queryKey: ['accounts'], queryFn: listAccounts })

  const { data: instances, isLoading, isError } = useQuery({
    queryKey: ['instances', accountKey, region, statuses, search, hideProtected],
    queryFn: () =>
      listInstances({
        accountKey,
        region: region || undefined,
        statuses: statuses.length > 0 ? statuses.join(',') : undefined,
        search: search || undefined,
        hideProtected: hideProtected || undefined,
      }),
    enabled: !!accountKey,
  })

  const actionMutation = useMutation({
    mutationFn: async ({ action, region, ids }: { action: 'start' | 'stop'; region: string; ids: string[] }) => {
      return action === 'start'
        ? startInstances(accountKey, region, ids, false)
        : stopInstances(accountKey, region, ids, false)
    },
    onSuccess: (result, variables) => {
      setActionMessage(
        `${variables.action === 'start' ? 'Started' : 'Stopped'} ${result.affected.length}, skipped ${result.skipped.length}.`
      )
      setSelected(new Set())
      queryClient.invalidateQueries({ queryKey: ['instances'] })
    },
    onError: () => setActionMessage('Action failed. Please try again.'),
  })

  const selectedInstances = useMemo(
    () => (instances ?? []).filter((i) => selected.has(i.instanceId)),
    [instances, selected]
  )

  // Bulk actions require all selected instances to share a region (API is region-scoped)
  const selectedRegions = new Set(selectedInstances.map((i) => i.region))
  const canBulkAct = selectedInstances.length > 0 && selectedRegions.size === 1

  function toggleSelected(id: string) {
    setSelected((prev) => {
      const next = new Set(prev)
      if (next.has(id)) next.delete(id)
      else next.add(id)
      return next
    })
  }

  function toggleSelectAll() {
    if (!instances) return
    if (selected.size === instances.length) {
      setSelected(new Set())
    } else {
      setSelected(new Set(instances.map((i) => i.instanceId)))
    }
  }

  function toggleStatus(s: string) {
    setStatuses((prev) => (prev.includes(s) ? prev.filter((x) => x !== s) : [...prev, s]))
  }

  function handleBulkAction(action: 'start' | 'stop') {
    if (!canBulkAct) return
    const region = [...selectedRegions][0]
    actionMutation.mutate({ action, region, ids: [...selected] })
  }

  return (
    <div className="min-h-screen bg-slate-50">
      <header className="bg-gradient-to-r from-indigo-600 to-purple-600 text-white px-6 py-4 flex justify-between items-center shadow">
        <h1 className="text-xl font-bold">Instance Manager</h1>
        <div className="flex items-center gap-4">
          <Link to="/logs" className="text-sm bg-white/20 hover:bg-white/30 rounded-lg px-3 py-1.5 transition">
            Logs
          </Link>
          <span className="text-sm opacity-90">{username}</span>
          <button onClick={logout} className="text-sm bg-white/20 hover:bg-white/30 rounded-lg px-3 py-1.5 transition">
            Logout
          </button>
        </div>
      </header>

      <main className="p-6 space-y-4">
        {/* Filter bar */}
        <div className="bg-white rounded-xl shadow p-4 space-y-3">
          <div className="flex flex-wrap gap-3 items-end">
            <div>
              <label className="block text-xs font-medium text-slate-500 mb-1">Account</label>
              <select
                value={accountKey}
                onChange={(e) => setAccountKey(e.target.value)}
                className="rounded-lg border border-slate-300 px-3 py-1.5 text-sm"
              >
                {(accounts ?? []).map((a) => (
                  <option key={a.key} value={a.key}>{a.name}</option>
                ))}
              </select>
            </div>

            <div>
              <label className="block text-xs font-medium text-slate-500 mb-1">Region</label>
              <input
                type="text"
                placeholder="All regions"
                value={region}
                onChange={(e) => setRegion(e.target.value)}
                className="rounded-lg border border-slate-300 px-3 py-1.5 text-sm w-36"
              />
            </div>

            <div className="flex-1 min-w-[200px]">
              <label className="block text-xs font-medium text-slate-500 mb-1">Search</label>
              <input
                type="text"
                placeholder="Instance ID, name, or IP…"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                className="w-full rounded-lg border border-slate-300 px-3 py-1.5 text-sm"
              />
            </div>

            <label className="flex items-center gap-2 text-sm text-slate-700 pb-1.5">
              <input type="checkbox" checked={hideProtected} onChange={(e) => setHideProtected(e.target.checked)} />
              Hide protected
            </label>
          </div>

          <div className="flex flex-wrap gap-2">
            {STATUS_OPTIONS.map((s) => (
              <button
                key={s}
                onClick={() => toggleStatus(s)}
                className={`text-xs px-2.5 py-1 rounded-full border transition ${
                  statuses.includes(s)
                    ? 'bg-indigo-600 text-white border-indigo-600'
                    : 'bg-white text-slate-600 border-slate-300 hover:border-indigo-400'
                }`}
              >
                {s}
              </button>
            ))}
          </div>
        </div>

        {/* Bulk actions bar */}
        {selected.size > 0 && (
          <div className="bg-indigo-50 border border-indigo-200 rounded-xl p-3 flex items-center justify-between">
            <span className="text-sm text-indigo-900">
              {selected.size} selected
              {!canBulkAct && <span className="text-red-600 ml-2">(must be same region to act)</span>}
            </span>
            <div className="flex gap-2">
              <button
                onClick={() => handleBulkAction('start')}
                disabled={!canBulkAct || actionMutation.isPending}
                className="text-sm bg-green-600 hover:bg-green-700 text-white rounded-lg px-3 py-1.5 disabled:opacity-50"
              >
                Start
              </button>
              <button
                onClick={() => handleBulkAction('stop')}
                disabled={!canBulkAct || actionMutation.isPending}
                className="text-sm bg-red-600 hover:bg-red-700 text-white rounded-lg px-3 py-1.5 disabled:opacity-50"
              >
                Stop
              </button>
              <button
                onClick={() => setSelected(new Set())}
                className="text-sm bg-white border border-slate-300 rounded-lg px-3 py-1.5"
              >
                Clear
              </button>
            </div>
          </div>
        )}

        {actionMessage && (
          <div className="bg-teal-50 border border-teal-200 text-teal-800 text-sm rounded-xl p-3">
            {actionMessage}
          </div>
        )}

        {/* Table */}
        <div className="bg-white rounded-xl shadow overflow-hidden">
          {isLoading ? (
            <div className="p-8 text-center text-slate-400">Loading instances…</div>
          ) : isError ? (
            <div className="p-8 text-center text-red-500">Failed to load instances.</div>
          ) : (instances ?? []).length === 0 ? (
            <div className="p-8 text-center text-slate-400">No instances found.</div>
          ) : (
            <table className="w-full text-sm">
              <thead className="bg-slate-50 border-b border-slate-200">
                <tr>
                  <th className="px-4 py-2 text-left">
                    <input
                      type="checkbox"
                      checked={instances!.length > 0 && selected.size === instances!.length}
                      onChange={toggleSelectAll}
                    />
                  </th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500">Name</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500">State</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500">Protected</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500">Public IP</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500">Private IP</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500">Region</th>
                </tr>
              </thead>
              <tbody>
                {(instances as Instance[]).map((inst, idx) => (
                  <tr
                    key={inst.instanceId}
                    onClick={() => setDetailsInstanceId(inst.instanceId)}
                    className={`border-b border-slate-100 hover:bg-indigo-50 transition cursor-pointer ${
                      idx % 2 === 0 ? 'bg-white' : 'bg-slate-50/50'
                    }`}
                  >
                    <td className="px-4 py-2" onClick={(e) => e.stopPropagation()}>
                      <input
                        type="checkbox"
                        checked={selected.has(inst.instanceId)}
                        onChange={() => toggleSelected(inst.instanceId)}
                      />
                    </td>
                    <td className="px-4 py-2">
                      <div className="font-medium text-slate-800">{inst.name}</div>
                      <div className="text-xs text-slate-400">{inst.instanceId}</div>
                    </td>
                    <td className="px-4 py-2"><StatusBadge state={inst.state} /></td>
                    <td className="px-4 py-2">
                      <span className={`text-xs px-2 py-0.5 rounded-full ${inst.dnsEnabled ? 'bg-amber-100 text-amber-800' : 'bg-slate-100 text-slate-500'}`}>
                        {inst.dnsEnabled ? 'True' : 'False'}
                      </span>
                    </td>
                    <td className="px-4 py-2 text-slate-600">{inst.publicIp ?? '—'}</td>
                    <td className="px-4 py-2 text-slate-600">{inst.privateIp ?? '—'}</td>
                    <td className="px-4 py-2 text-slate-600">{inst.region}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </main>

      {detailsInstanceId && (
        <InstanceDetailsModal
          instanceId={detailsInstanceId}
          accountKey={accountKey}
          region={(instances ?? []).find((i) => i.instanceId === detailsInstanceId)?.region ?? region}
          onClose={() => setDetailsInstanceId(null)}
        />
      )}
    </div>
  )
}
