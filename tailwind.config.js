/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    './Pages/**/*.cshtml',
    './Views/**/*.cshtml'
  ],
  safelist: [
    'bg-yellow-300',
    'bg-green-300'
  ],
  theme: {
    extend: {},
  },
  plugins: [],
};