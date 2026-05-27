const axios = require("axios");
const crypto = require("crypto");

const apiKey = process.env.BINANCE_API_KEY;
const apiSecret = process.env.BINANCE_API_SECRET;
const destination = process.env.DESTINATION_WALLET;
const amount = process.env.AMOUNT;
const symbol = process.env.SYMBOL;

const timestamp = Date.now();
const query = `asset=${symbol}&amount=${amount}&address=${destination}&timestamp=${timestamp}`;
const signature = crypto.createHmac("sha256", apiSecret).update(query).digest("hex");

axios.post("https://api.binance.com/sapi/v1/capital/withdraw/apply", null, {
  params: { asset: symbol, amount, address: destination, timestamp, signature },
  headers: { "X-MBX-APIKEY": apiKey }
})
.then(res => console.log("[SUCCESS] Transferencia realizada:", res.data))
.catch(err => console.error("[ERROR]", err.response.data));
