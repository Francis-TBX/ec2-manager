import { useEffect, useRef, useState } from 'react'

interface Props {
  label: string
  options: { value: string; label: string }[]
  selected: string[]
  onChange: (next: string[]) => void
  placeholder?: string
}

export default function MultiSelectDropdown({ label, options, selected, onChange, placeholder = 'All' }: Props) {
  const [open, setOpen] = useState(false)
  const ref = useRef<HTMLDivElement>(null)

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (ref.current && !ref.current.contains(e.target as Node)) {
        setOpen(false)
      }
    }
    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [])

  function toggle(value: string) {
    onChange(selected.includes(value) ? selected.filter((v) => v !== value) : [...selected, value])
  }

  const buttonText =
    selected.length === 0
      ? placeholder
      : selected.length === 1
      ? options.find((o) => o.value === selected[0])?.label ?? selected[0]
      : `${selected.length} selected`

  return (
    <div className="relative" ref={ref}>
      <label className="block text-xs font-medium text-slate-500 mb-1">{label}</label>
      <button
        type="button"
        onClick={() => setOpen((o) => !o)}
        className={`rounded-lg border px-3 py-1.5 text-sm text-left w-40 flex justify-between items-center ${
          selected.length > 0 ? 'border-indigo-400 bg-indigo-50 text-indigo-700' : 'border-slate-300 bg-white text-slate-700'
        }`}
      >
        <span className="truncate">{buttonText}</span>
        <span className="text-slate-400 ml-1">▾</span>
      </button>

      {open && (
        <div className="absolute z-20 mt-1 w-56 max-h-64 overflow-y-auto bg-white border border-slate-200 rounded-lg shadow-lg p-1.5">
          {options.length === 0 ? (
            <div className="text-xs text-slate-400 px-2 py-1.5">No options</div>
          ) : (
            <>
              {selected.length > 0 && (
                <button
                  type="button"
                  onClick={() => onChange([])}
                  className="w-full text-left text-xs text-indigo-600 hover:bg-indigo-50 rounded px-2 py-1 mb-0.5"
                >
                  Clear
                </button>
              )}
              {options.map((opt) => (
                <label
                  key={opt.value}
                  className="flex items-center gap-2 text-sm px-2 py-1.5 rounded hover:bg-slate-50 cursor-pointer"
                >
                  <input
                    type="checkbox"
                    checked={selected.includes(opt.value)}
                    onChange={() => toggle(opt.value)}
                  />
                  {opt.label}
                </label>
              ))}
            </>
          )}
        </div>
      )}
    </div>
  )
}
