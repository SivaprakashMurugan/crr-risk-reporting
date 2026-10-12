import { useEffect, useState } from 'react'
import { fetchPositionByTradeId } from '../../api/positionsApi'
import { formatCurrency, formatNumber } from '../grid/formatters'
import type { RiskPosition } from '../../types/risk'

interface DetailDrawerProps {
  tradeId: string | null
  onClose: () => void
}

export function DetailDrawer({ tradeId, onClose }: Readonly<DetailDrawerProps>) {
  const [data, setData] = useState<RiskPosition | null>(null)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!tradeId) {
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setData(null)
      setError(null)
      return
    }

    const controller = new AbortController()
    setIsLoading(true)
    setError(null)

    fetchPositionByTradeId(tradeId, controller.signal)
      .then(setData)
      .catch((exception: unknown) => {
        if (controller.signal.aborted || (exception instanceof DOMException && exception.name === 'AbortError')) {
          return
        }

        setError(exception instanceof Error ? exception.message : 'Failed to load detail')
      })
      .finally(() => {
        if (!controller.signal.aborted) {
          setIsLoading(false)
        }
      })

    return () => controller.abort()
  }, [tradeId])

  const isOpen = Boolean(tradeId)
  const drawerClassName = isOpen ? 'detail-drawer open' : 'detail-drawer closed'

  return (
    <aside className={drawerClassName} aria-hidden={!isOpen}>
      <header>
        <h3>Trade detail</h3>
        <button type="button" onClick={onClose}>
          Close
        </button>
      </header>

      {isLoading && <p>Loading trade details...</p>}
      {error && <p className="error">{error}</p>}

      {data && (
        <dl>
          <div><dt>Trade ID</dt><dd>{data.tradeId}</dd></div>
          <div><dt>Book</dt><dd>{data.book}</dd></div>
          <div><dt>Commodity</dt><dd>{data.commodity}</dd></div>
          <div><dt>Counterparty</dt><dd>{data.counterparty}</dd></div>
          <div><dt>Region</dt><dd>{data.region}</dd></div>
          <div><dt>Instrument</dt><dd>{data.instrumentType}</dd></div>
          <div><dt>Position</dt><dd>{formatNumber(data.position)}</dd></div>
          <div><dt>Currency</dt><dd>{data.currency}</dd></div>
          <div><dt>Market Price</dt><dd>{formatNumber(data.marketPrice)}</dd></div>
          <div><dt>P&amp;L</dt><dd>{formatCurrency(data.pnl)}</dd></div>
          <div><dt>FX Exposure</dt><dd>{formatCurrency(data.fxExposure)}</dd></div>
          <div><dt>As Of Date</dt><dd>{data.asOfDate}</dd></div>
        </dl>
      )}
    </aside>
  )
}
