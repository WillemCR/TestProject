/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    './Pages/**/*.cshtml',
    './Views/**/*.cshtml'
  ],
  safelist: [
    'bg-yellow-300',
    'bg-green-300',
    'bg-blue-500',
    'bg-blue-600',
    'bg-blue-700',
    'bg-green-500',
    'bg-green-600',
    'bg-green-700',
        
  ],
  theme: {
    extend: {},
  },
  plugins: [],
};