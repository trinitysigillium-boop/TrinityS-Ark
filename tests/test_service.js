const request = require('supertest');
describe('API Health', () => {
  it('should return 200', async () => {
    const res = await request('http://localhost:3000').get('/health');
    expect(res.statusCode).toEqual(200);
  });
});
