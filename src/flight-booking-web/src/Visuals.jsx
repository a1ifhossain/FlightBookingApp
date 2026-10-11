export function Icon({ name, ...props }) {
  const paths = {
    plane: 'M22 2 9 15M22 2l-8 20-5-7-7-5 20-8Z',
    arrow: 'M4 12h16m-6-6 6 6-6 6',
    swap: 'M4 8h16m-4-4 4 4-4 4M20 16H4m4-4-4 4 4 4',
    calendar: 'M8 2v4m8-4v4M3 10h18M5 4h14a2 2 0 0 1 2 2v14H3V6a2 2 0 0 1 2-2Z',
    search: 'M21 21l-5-5M10 3a7 7 0 1 0 0 14 7 7 0 0 0 0-14Z',
    pin: 'M20 10c0 6-8 12-8 12S4 16 4 10a8 8 0 1 1 16 0ZM12 7a3 3 0 1 0 0 6 3 3 0 0 0 0-6Z',
    shield: 'M12 2 3 6v6c0 6 9 10 9 10s9-4 9-10V6l-9-4ZM8 12l3 3 5-6',
    user: 'M20 21v-2a6 6 0 0 0-6-6h-4a6 6 0 0 0-6 6v2M12 3a4 4 0 1 0 0 8 4 4 0 0 0 0-8Z',
    ticket: 'M3 5h18v5a2 2 0 0 0 0 4v5H3v-5a2 2 0 0 0 0-4V5ZM15 5v3m0 3v2m0 3v3',
    info: 'M12 11v6m0-10v.1M12 2a10 10 0 1 0 0 20 10 10 0 0 0 0-20Z',
    compass: 'M12 2a10 10 0 1 0 0 20 10 10 0 0 0 0-20ZM16 8l-3 5-5 3 3-5 5-3Z',
    sort: 'M8 4v16m-4-4 4 4 4-4M16 4h5m-5 5h4m-4 5h3',
  };
  return <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true" {...props}><path d={paths[name] || paths.plane}/></svg>;
}

export function Scenic({ compact = false, hills = false }) {
  // Original vector artwork, so the site needs no third-party photo downloads.
  return <svg className={`scenic ${hills ? 'scenic-hills' : ''}`} viewBox="0 0 600 460" role="img" aria-label={hills ? 'Illustrated green hills and a coastal city' : 'Illustrated coastline with waves, palms and a flight path'}>
    <rect width="600" height="460" fill={hills ? '#dcf1ed' : '#d9eff9'}/>
    <circle cx="451" cy="99" r="48" fill={hills ? '#f4d786' : '#ffe4b0'}/>
    <path d="M0 196Q60 110 140 158T291 139Q373 69 470 158T600 135V340H0Z" fill={hills ? '#a5c6b3' : '#b9dbe5'}/>
    <path d="M0 241Q95 163 166 212T330 189Q408 141 485 204T600 174V368H0Z" fill={hills ? '#689c87' : '#84becc'}/>
    <path d="M0 276Q103 235 215 266T411 249Q530 231 600 270V460H0Z" fill={hills ? '#478576' : '#4b9eae'}/>
    <path d="M0 305Q88 278 206 310T393 298Q513 279 600 310V460H0Z" fill="#268ca6"/>
    <path d="M0 358Q109 283 201 331T407 354Q526 374 600 313V460H0Z" fill="#f0ddbd"/>
    <path d="M0 346Q110 276 201 322T407 344Q526 361 600 303" fill="none" stroke="#fff9e9" strokeWidth="8"/>
    <path d="M320 293q64-21 110-6m-384 6q55-15 92-6m324 24q54 15 94-4" stroke="#b7e4e8" strokeWidth="3" fill="none" opacity=".75"/>
    <path d="M81 460q27-88 15-129M61 338q37-37 61 0m-54-14q21-34 51-2M87 328q28-20 60 2M97 336q-23-36-55-13" stroke="#205f58" fill="none" strokeWidth="9" strokeLinecap="round"/>
    <path d="M499 460q-7-54-3-88m-28 7q29-32 53-3m-28-4q23-20 45-5m-44 2q-19-25-44-10" stroke="#2d6e5c" fill="none" strokeWidth="7" strokeLinecap="round"/>
    <path d="M113 123c42-74 137-60 214-29s131 5 166-18" fill="none" stroke="#fff" strokeWidth="2" strokeDasharray="6 8" opacity=".7"/>
    {!compact && <g transform="translate(312 94) rotate(-18)"><path d="M-50 0 16-13 11-34 23-35 36-16 65-9 72 0 33 4 11 28 0 27 6 6-38 11-50 0Z" fill="#fff"/><path d="m36-16 29 7 7 9-39 4" fill="#cfe2ed"/></g>}
    {hills && <g fill="#eff4df"><rect x="233" y="233" width="25" height="66" rx="2"/><rect x="266" y="249" width="37" height="50" rx="2"/><rect x="309" y="214" width="23" height="85" rx="2"/><rect x="343" y="242" width="31" height="57" rx="2"/></g>}
    <g fill="white" opacity=".75"><ellipse cx="85" cy="64" rx="31" ry="8"/><ellipse cx="110" cy="60" rx="24" ry="12"/><ellipse cx="361" cy="44" rx="25" ry="7"/><ellipse cx="382" cy="39" rx="20" ry="10"/></g>
  </svg>;
}
