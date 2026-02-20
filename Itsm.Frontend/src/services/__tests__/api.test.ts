import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import {
  fetchComputers,
  fetchComputer,
  fetchDiskUsage,
  fetchAgents,
  fetchAgent,
  requestUpdate,
  fetchLogs,
  fetchAssets,
  fetchAsset,
  createAsset,
  updateAsset,
  deleteAsset,
} from '../api'

const mockFetch = vi.fn()

beforeEach(() => {
  global.fetch = mockFetch
})

afterEach(() => {
  vi.restoreAllMocks()
})

function okResponse(data: unknown) {
  return Promise.resolve({
    ok: true,
    json: () => Promise.resolve(data),
  } as Response)
}

function errorResponse(status: number) {
  return Promise.resolve({
    ok: false,
    status,
  } as Response)
}

describe('fetchComputers', () => {
  it('calls /inventory/computers and returns data', async () => {
    const data = [{ computerName: 'PC1' }]
    mockFetch.mockReturnValue(okResponse(data))
    const result = await fetchComputers()
    expect(mockFetch).toHaveBeenCalledWith('/inventory/computers')
    expect(result).toEqual(data)
  })

  it('throws on non-ok response', async () => {
    mockFetch.mockReturnValue(errorResponse(500))
    await expect(fetchComputers()).rejects.toThrow('Failed to fetch computers: 500')
  })
})

describe('fetchComputer', () => {
  it('calls /inventory/computers/{name}', async () => {
    const data = { computerName: 'PC1' }
    mockFetch.mockReturnValue(okResponse(data))
    const result = await fetchComputer('PC1')
    expect(mockFetch).toHaveBeenCalledWith('/inventory/computers/PC1')
    expect(result).toEqual(data)
  })

  it('encodes computer name', async () => {
    mockFetch.mockReturnValue(okResponse({}))
    await fetchComputer('my pc')
    expect(mockFetch).toHaveBeenCalledWith('/inventory/computers/my%20pc')
  })

  it('throws on error', async () => {
    mockFetch.mockReturnValue(errorResponse(404))
    await expect(fetchComputer('x')).rejects.toThrow('Failed to fetch computer: 404')
  })
})

describe('fetchDiskUsage', () => {
  it('calls /inventory/disk-usage/{name}', async () => {
    mockFetch.mockReturnValue(okResponse({ computerName: 'PC1' }))
    await fetchDiskUsage('PC1')
    expect(mockFetch).toHaveBeenCalledWith('/inventory/disk-usage/PC1')
  })
})

describe('fetchAgents', () => {
  it('calls /agents', async () => {
    const data = [{ hardwareUuid: 'abc' }]
    mockFetch.mockReturnValue(okResponse(data))
    const result = await fetchAgents()
    expect(mockFetch).toHaveBeenCalledWith('/agents')
    expect(result).toEqual(data)
  })

  it('throws on error', async () => {
    mockFetch.mockReturnValue(errorResponse(500))
    await expect(fetchAgents()).rejects.toThrow('Failed to fetch agents: 500')
  })
})

describe('fetchAgent', () => {
  it('calls /agents/{uuid}', async () => {
    mockFetch.mockReturnValue(okResponse({ hardwareUuid: 'abc' }))
    await fetchAgent('abc')
    expect(mockFetch).toHaveBeenCalledWith('/agents/abc')
  })
})

describe('requestUpdate', () => {
  it('sends POST with update type', async () => {
    mockFetch.mockReturnValue(okResponse(undefined))
    await requestUpdate('abc', 'Inventory')
    expect(mockFetch).toHaveBeenCalledWith('/agents/abc/request-update', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify('Inventory'),
    })
  })

  it('throws on error', async () => {
    mockFetch.mockReturnValue(errorResponse(400))
    await expect(requestUpdate('abc', 'Inventory')).rejects.toThrow('Failed to request update: 400')
  })
})

describe('fetchLogs', () => {
  it('calls /agents/{uuid}/logs', async () => {
    const data = [{ message: 'hello' }]
    mockFetch.mockReturnValue(okResponse(data))
    const result = await fetchLogs('abc')
    expect(mockFetch).toHaveBeenCalledWith('/agents/abc/logs')
    expect(result).toEqual(data)
  })
})

describe('fetchAssets', () => {
  it('calls /assets with no params', async () => {
    mockFetch.mockReturnValue(okResponse([]))
    await fetchAssets()
    expect(mockFetch).toHaveBeenCalledWith('/assets')
  })

  it('builds query string with type', async () => {
    mockFetch.mockReturnValue(okResponse([]))
    await fetchAssets({ type: 'Computer' })
    expect(mockFetch).toHaveBeenCalledWith('/assets?type=Computer')
  })

  it('builds query string with all params', async () => {
    mockFetch.mockReturnValue(okResponse([]))
    await fetchAssets({ type: 'Monitor', status: 'InUse', search: 'test' })
    const url = mockFetch.mock.lastCall![0] as string
    expect(url).toContain('type=Monitor')
    expect(url).toContain('status=InUse')
    expect(url).toContain('search=test')
  })

  it('skips empty params', async () => {
    mockFetch.mockReturnValue(okResponse([]))
    await fetchAssets({ type: '', status: '' })
    expect(mockFetch).toHaveBeenCalledWith('/assets')
  })

  it('throws on error', async () => {
    mockFetch.mockReturnValue(errorResponse(500))
    await expect(fetchAssets()).rejects.toThrow('Failed to fetch assets: 500')
  })
})

describe('fetchAsset', () => {
  it('calls /assets/{id}', async () => {
    mockFetch.mockReturnValue(okResponse({ id: '123' }))
    const result = await fetchAsset('123')
    expect(mockFetch).toHaveBeenCalledWith('/assets/123')
    expect(result).toEqual({ id: '123' })
  })

  it('encodes id', async () => {
    mockFetch.mockReturnValue(okResponse({}))
    await fetchAsset('a b')
    expect(mockFetch).toHaveBeenCalledWith('/assets/a%20b')
  })

  it('throws on error', async () => {
    mockFetch.mockReturnValue(errorResponse(404))
    await expect(fetchAsset('x')).rejects.toThrow('Failed to fetch asset: 404')
  })
})

describe('createAsset', () => {
  it('sends POST with JSON body', async () => {
    const asset = { name: 'Monitor', type: 'Monitor' }
    mockFetch.mockReturnValue(okResponse({ id: '1', ...asset }))
    const result = await createAsset(asset)
    expect(mockFetch).toHaveBeenCalledWith('/assets', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(asset),
    })
    expect(result).toEqual({ id: '1', ...asset })
  })

  it('throws on error', async () => {
    mockFetch.mockReturnValue(errorResponse(400))
    await expect(createAsset({})).rejects.toThrow('Failed to create asset: 400')
  })
})

describe('updateAsset', () => {
  it('sends PUT with JSON body', async () => {
    const asset = { name: 'Updated' }
    mockFetch.mockReturnValue(okResponse({ id: '1', name: 'Updated' }))
    const result = await updateAsset('1', asset)
    expect(mockFetch).toHaveBeenCalledWith('/assets/1', {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(asset),
    })
    expect(result).toEqual({ id: '1', name: 'Updated' })
  })

  it('encodes id', async () => {
    mockFetch.mockReturnValue(okResponse({}))
    await updateAsset('a b', { name: 'x' })
    expect(mockFetch).toHaveBeenCalledWith('/assets/a%20b', expect.any(Object))
  })

  it('throws on error', async () => {
    mockFetch.mockReturnValue(errorResponse(500))
    await expect(updateAsset('1', {})).rejects.toThrow('Failed to update asset: 500')
  })
})

describe('deleteAsset', () => {
  it('sends DELETE', async () => {
    mockFetch.mockReturnValue(okResponse(undefined))
    await deleteAsset('1')
    expect(mockFetch).toHaveBeenCalledWith('/assets/1', { method: 'DELETE' })
  })

  it('encodes id', async () => {
    mockFetch.mockReturnValue(okResponse(undefined))
    await deleteAsset('a b')
    expect(mockFetch).toHaveBeenCalledWith('/assets/a%20b', { method: 'DELETE' })
  })

  it('throws on error', async () => {
    mockFetch.mockReturnValue(errorResponse(500))
    await expect(deleteAsset('1')).rejects.toThrow('Failed to delete asset: 500')
  })
})
