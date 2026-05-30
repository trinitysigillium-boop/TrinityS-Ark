import WebSocket from 'ws';
import http from 'http';

const EXCHANGES = { BITSO: 'wss://ws.bitso.com' };

function conectarBitso() {
  const ws = new WebSocket(EXCHANGES.BITSO);

  ws.on('open', () => {
    console.log('\x1b[36m[WebSocket]\x1b[0m Conectado a Bitso Stream. Suscribiendo btc_mxn...');
    ws.send(JSON.stringify({ action: 'subscribe', book: 'btc_mxn', type: 'trades' }));
  });

  ws.on('message', (data) => {
    const response = JSON.parse(data);
    if (response.type === 'trades' && response.payload) {
      const trade = response.payload[0];
      if (trade) {
        console.log(`\x1b[33m[Mercado Real]\x1b[0m Rate: ${trade.rate} MXN`);
        enviarAlBackend('BTC_MXN', parseFloat(trade.rate) * 0.999, parseFloat(trade.rate) * 1.001);
      }
    }
  });

  ws.on('error', (err) => console.error('[WS Error]', err.message));
  ws.on('close', () => setTimeout(conectarBitso, 5000));
}

function enviarAlBackend(pair, bid, ask) {
  const data = JSON.stringify({ Pair: pair, Bid: bid, Ask: ask });
  const req = http.request({
    hostname: 'localhost',
    port: 5000,
    path: '/api/v1/liquidity/update',
    method: 'POST',
    headers: { 'Content-Type': 'application/json', 'Content-Length': data.length }
  }, (res) => { res.on('data', () => {}); });
  
  req.on('error', () => {});
  req.write(data);
  req.end();
}

conectarBitso();
