import { useState } from 'react';
import { Icon, Scenic } from './Visuals.jsx';
import { api, setToken } from './api.js';
import './styles.css';

const airports = [['DAC', 'Dhaka'], ['CXB', 'Cox’s Bazar'], ['CGP', 'Chattogram']];
const money = amount => new Intl.NumberFormat('en-BD', { style: 'currency', currency: 'BDT', maximumFractionDigits: 0 }).format(amount);
function dateInBangladesh(days = 0) {
  const parts = new Intl.DateTimeFormat('en-US', { timeZone: 'Asia/Dhaka', year: 'numeric', month: '2-digit', day: '2-digit' }).formatToParts(new Date(Date.now() + days * 86400000));
  const part = type => parts.find(x => x.type === type).value;
  return `${part('year')}-${part('month')}-${part('day')}`;
}
function ErrorMessage({ message }) { return message ? <p className="error" role="alert">{message}</p> : null; }

export default function App() {
  const [user, setUser] = useState(null);
  const [view, setView] = useState('search');
  const [selected, setSelected] = useState(null);
  const [booking, setBooking] = useState(null);
  function logout() { setToken(''); setUser(null); setBooking(null); setSelected(null); setView('search'); }
  function select(offer) { setSelected(offer); setView(user ? 'create' : 'login'); }
  return <div className="site-shell">
    <a className="skip-link" href="#main-content">Skip to content</a>
    <header className="site-header"><div className="header-inner">
      <a className="brand" href="/" aria-label="Flight Booking App home"><span className="brand-symbol"><Icon name="plane"/></span><span>Flight<span className="brand-accent">Booking</span><small>Every journey starts here.</small></span></a>
      <nav aria-label="Main navigation"><button className={view === 'search' ? 'active' : ''} onClick={() => setView('search')}>Explore flights</button>
        {user && <button className={view === 'bookings' || view === 'detail' ? 'active' : ''} onClick={() => { setBooking(null); setView('bookings'); }}>{user.roles.includes('Agent') ? 'Agent bookings' : user.roles.includes('Admin') ? 'All bookings' : 'My bookings'}</button>}
        {user?.roles.includes('Admin') && <button className={view === 'admin' ? 'active' : ''} onClick={() => setView('admin')}>Admin</button>}
      </nav>
      <div className="header-account">{user ? <><span className="account"><Icon name="user"/><span>{user.email}<small>{user.roles.join(', ')}</small></span></span><button className="secondary compact" onClick={logout}>Sign out</button></> : <button className="login-button" onClick={() => setView('login')}><Icon name="user"/>Sign in / Register</button>}</div>
    </div></header>
    <main id="main-content" className={`app-content view-${view}`}>
      {view === 'search' && <Search onSelect={select} />}
      {view === 'login' && <Auth onSuccess={account => { setUser(account); setView(selected ? 'create' : 'search'); }} />}
      {view === 'create' && user && selected && <CreateBooking offer={selected} email={user.email} onCreated={value => { setBooking(value); setSelected(null); setView('detail'); }} onCancel={() => setView('search')} />}
      {view === 'bookings' && user && <BookingList onOpen={value => { setBooking(value); setView('detail'); }} />}
      {view === 'detail' && booking && user && <BookingDetail initial={booking} onBack={() => setView('bookings')} />}
      {view === 'admin' && user?.roles.includes('Admin') && <Admin />}
    </main>
    <footer className="site-footer"><div><span className="footer-brand"><Icon name="plane"/>FlightBooking</span><p>A little curiosity. A new destination. Your next journey.</p></div><div className="footer-note"><span className="demo-dot"/>Portfolio demo · Fictional flights & payments<p>Built for learning travel technology.</p></div></footer>
  </div>;
}

function Search({ onSelect }) {
  const [criteria, setCriteria] = useState({ origin: 'DAC', destination: 'CXB', departureDate: dateInBangladesh(7) });
  const [result, setResult] = useState(null);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const [searched, setSearched] = useState(null);
  const [filter, setFilter] = useState('All');
  async function submit(event) {
    event.preventDefault(); setError(''); setResult(null); setFilter('All');
    if (criteria.origin === criteria.destination) { setError('Choose different airports.'); return; }
    setBusy(true);
    try { const value = await api(`/flights/search?${new URLSearchParams(criteria)}`); setResult(value); setSearched({ ...criteria }); }
    catch (e) { setError(e.message); } finally { setBusy(false); }
  }
  function update(event) { setCriteria(current => ({ ...current, [event.target.name]: event.target.value })); }
  function chooseRoute(destination) {
    setCriteria(current => ({ ...current, origin: 'DAC', destination }));
    document.getElementById('flight-search').scrollIntoView({ behavior: 'smooth', block: 'center' });
    document.getElementById('departure-date').focus({ preventScroll: true });
  }
  const offers = result?.offers.filter(offer => filter === 'All' || offer.supplier === filter) || [];
  return <>
    <section className="hero">
      <div className="hero-copy"><span className="hero-kicker"><span/>A NEW PERSPECTIVE AWAITS</span><h1>Go somewhere<br/><em>worth remembering.</em></h1><p>From the coast to the hills, discover your next corner of Bangladesh. Find your flight and make a little room for adventure.</p><a className="hero-link" href="#flight-search">Let’s find your next flight <Icon name="arrow"/></a><div className="hero-demo"><Icon name="shield"/>Explore with fictional airlines and fares</div></div>
      <div className="hero-art"><Scenic/><div className="destination-label"><Icon name="pin"/><span>Cox’s Bazar<small>Sea air. Wide horizons.</small></span></div><span className="art-caption">BANGLADESH, FROM A NEW ANGLE</span></div>
    </section>
    <section className="panel search-panel" id="flight-search" aria-label="Flight search">
      <div className="search-topline"><span className="search-tab"><Icon name="plane"/>One-way flights</span><span className="subtle">One trip. Endless possibilities.</span></div>
      <form onSubmit={submit}><fieldset disabled={busy} className="flight-search-grid">
        {['origin', 'destination'].map(name => <label key={name}><span><Icon name="pin"/>{name === 'origin' ? 'From' : 'To'}</span><select name={name} value={criteria[name]} onChange={update}>{airports.map(([code, city]) => <option key={code} value={code}>{city} ({code})</option>)}</select></label>)}
        <button type="button" className="swap-button" aria-label="Swap departure and destination" onClick={() => setCriteria(current => ({ ...current, origin: current.destination, destination: current.origin }))}><Icon name="swap"/></button>
        <label><span><Icon name="calendar"/>Departure date</span><input id="departure-date" type="date" name="departureDate" required min={dateInBangladesh()} value={criteria.departureDate} onChange={update}/></label>
        <button className="search-submit">{busy ? <><span className="spinner"/>Searching…</> : <><Icon name="search"/>Search flights</>}</button>
      </fieldset></form><div className="search-bottomline"><span><Icon name="info"/>Sample routes: DAC → CXB and DAC → CGP</span><span>Fares shown per passenger</span></div>
    </section>
    <section className="results" aria-live="polite" aria-busy={busy}><ErrorMessage message={error}/>
      {busy && <div className="loading-card"><span className="spinner"/><div><strong>Finding your next journey</strong><p>Checking offers from both suppliers…</p></div></div>}
      {result && <><div className="section-heading"><div><p className="eyebrow">YOUR FLIGHT OPTIONS</p><h2>{result.offers.length} flight offers</h2><p>{searched.origin} → {searched.destination} · {searched.departureDate}</p></div><span className="sort-note"><Icon name="sort"/>Lowest fare first</span></div>
        {result.warnings.map(w => <p className="notice" key={w}>{w}</p>)}
        <div className="supplier-filters" aria-label="Supplier filters">{['All','REST','SOAP'].map(value => <button key={value} className={filter === value ? 'selected' : ''} aria-pressed={filter === value} onClick={() => setFilter(value)}>{value === 'All' ? 'All airlines' : `${value} supplier`}<span>{value === 'All' ? result.offers.length : result.offers.filter(o => o.supplier === value).length}</span></button>)}</div>
        {!offers.length && <div className="empty-state"><Icon name="compass"/><h3>No flights on this route yet</h3><p>Our sample destinations are Cox’s Bazar and Chattogram, departing from Dhaka.</p><button className="secondary" onClick={() => chooseRoute('CXB')}>Try Dhaka → Cox’s Bazar</button></div>}
        {offers.map((offer) => <article className="offer flight-offer" key={`${offer.supplier}-${offer.id}`}>
          <div className="airline-identity"><span className={`airline-symbol ${offer.supplier.toLowerCase()}`}><Icon name="plane"/></span><div><strong>{offer.airline}</strong><span>Mock airline · {offer.supplier}</span></div></div>
          <div className="flight-route"><div><h3>{offer.origin}</h3><span>{airports.find(([code]) => code === offer.origin)?.[1] || offer.origin}</span></div><div className="route-line"><Icon name="plane"/><span>One-way journey</span></div><div><h3>{offer.destination}</h3><span>{airports.find(([code]) => code === offer.destination)?.[1] || offer.destination}</span></div><p><Icon name="calendar"/>{offer.departureDate}<span className="seats">{offer.availableSeats} sample seats</span></p></div>
          <div className="fare"><small>Per passenger</small><strong>{money(offer.price)}</strong><button onClick={() => onSelect(offer)}>Select flight<Icon name="arrow"/></button></div>
        </article>)}</>}
    </section>
    {!result && !busy && <section className="destinations"><div className="section-heading"><div><p className="eyebrow">A LITTLE TRAVEL INSPIRATION</p><h2>Closer than you think.</h2><p>A coastline to unwind. A city to discover. Where will you go?</p></div><span className="subtle">Explore our sample routes</span></div><div className="destination-grid">
      <button className="destination-card coast" onClick={() => chooseRoute('CXB')}><div className="destination-art"><Scenic compact/></div><div className="destination-copy"><span>THE COAST IS CALLING</span><h3>Cox’s Bazar</h3><p>Dhaka → Cox’s Bazar · DAC–CXB</p><span className="destination-arrow"><Icon name="arrow"/></span></div></button>
      <button className="destination-card hills" onClick={() => chooseRoute('CGP')}><div className="destination-art"><Scenic compact hills/></div><div className="destination-copy"><span>TAKE THE SCENIC ROUTE</span><h3>Chattogram</h3><p>Dhaka → Chattogram · DAC–CGP</p><span className="destination-arrow"><Icon name="arrow"/></span></div></button>
    </div></section>}
    <section className="journey-strip"><div><span className="feature-icon"><Icon name="search"/></span><span><strong>Find your flight</strong><small>Compare offers in one place</small></span></div><div><span className="feature-icon"><Icon name="ticket"/></span><span><strong>Your trip, together</strong><small>Book for up to six passengers</small></span></div><div><span className="feature-icon"><Icon name="shield"/></span><span><strong>Stay in the loop</strong><small>Follow every booking milestone</small></span></div></section>
  </>;
}
function Auth({ onSuccess }) {
  const [register, setRegister] = useState(false);
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  async function submit(event) {
    event.preventDefault(); setBusy(true); setError('');
    try {
      if (register) await api('/auth/register', { method: 'POST', body: { email, password } });
      const session = await api('/auth/login?useCookies=false', { method: 'POST', body: { email, password } });
      setToken(session.accessToken);
      onSuccess(await api('/account/me'));
    } catch (e) { setToken(''); setError(e.message); } finally { setBusy(false); }
  }
  return <div className="auth-layout"><aside className="auth-story"><span className="hero-kicker">YOUR JOURNEY, ALL TOGETHER</span><h1>A world of plans.<br/><em>One place to begin.</em></h1><p>Keep your passengers, bookings and tickets together, from the first search to the next adventure.</p><Scenic compact/><div className="auth-feature"><Icon name="ticket"/>Your bookings, always within reach</div></aside><section className="panel narrow"><p className="eyebrow">YOUR ACCOUNT</p><h2>{register ? 'Create an account' : 'Welcome back'}</h2><ErrorMessage message={error}/>
    <form onSubmit={submit}><fieldset disabled={busy} className="stack"><label>Email<input required type="email" value={email} onChange={e => setEmail(e.target.value)} autoComplete="username"/></label>
      <label>Password<input required type="password" minLength={register ? 10 : undefined} value={password} onChange={e => setPassword(e.target.value)} autoComplete={register ? 'new-password' : 'current-password'}/></label>
      {register && <p className="hint">At least 10 characters, with uppercase, lowercase, a digit and a symbol. Use a practice password.</p>}
      <button>{busy ? 'Please wait…' : register ? 'Register and sign in' : 'Sign in'}</button></fieldset></form>
    <button className="secondary" onClick={() => { setRegister(!register); setError(''); }}>{register ? 'Already registered? Sign in' : 'New here? Register'}</button>
    <p className="hint demo-account-note"><Icon name="info"/>Demo access: use your customer, agent or admin account and the local demo password.</p>
  </section></div>;
}

function CreateBooking({ offer, email: defaultEmail, onCreated, onCancel }) {
  const [names, setNames] = useState(['']);
  const [email, setEmail] = useState(defaultEmail);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const [attempt, setAttempt] = useState(null);
  async function submit(event) {
    event.preventDefault(); setError(''); setBusy(true);
    const body = { offerId: offer.id, supplier: offer.supplier, origin: offer.origin, destination: offer.destination, departureDate: offer.departureDate, contactEmail: email.trim(), passengers: names.map(fullName => ({ fullName: fullName.trim() })) };
    // Keep the same key for an unchanged retry after a lost response.
    const serialized = JSON.stringify(body);
    const key = attempt?.serialized === serialized ? attempt.key : crypto.randomUUID();
    setAttempt({ serialized, key });
    try { onCreated(await api('/bookings', { method: 'POST', body, headers: { 'Idempotency-Key': key } })); }
    catch (e) { setError(e.message); } finally { setBusy(false); }
  }
  return <section className="panel booking-form-panel"><p className="eyebrow">BOOK YOUR JOURNEY</p><h2>Passenger details</h2><p>{offer.airline} · {offer.origin} → {offer.destination} · {offer.departureDate}</p>
    <p className="notice">Use fictional details. This creates a Pending booking; confirmation and mock payment happen separately.</p><ErrorMessage message={error}/>
    <form onSubmit={submit}><fieldset disabled={busy} className="stack">
      {names.map((name, i) => <label key={i}>Passenger {i + 1}<input required minLength={2} maxLength={100} value={name} onChange={e => setNames(current => current.map((value, n) => n === i ? e.target.value : value))} placeholder="Demo Passenger"/></label>)}
      <div className="actions"><button type="button" className="secondary" disabled={names.length >= Math.min(6, offer.availableSeats)} onClick={() => setNames([...names, ''])}>Add passenger</button>{names.length > 1 && <button type="button" className="secondary" onClick={() => setNames(names.slice(0, -1))}>Remove last passenger</button>}</div>
      <label>Contact email<input required type="email" maxLength={254} value={email} onChange={e => setEmail(e.target.value)}/></label>
      <p>Estimated total: <strong>{money(offer.price * names.length)}</strong>. The backend rechecks the fare.</p>
      <div className="actions"><button>{busy ? 'Creating…' : 'Create mock booking'}</button><button type="button" className="secondary" onClick={onCancel}>Back to search</button></div>
    </fieldset></form></section>;
}

function BookingList({ onOpen }) {
  const [result, setResult] = useState(null);
  const [page, setPage] = useState(1);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  async function load(next = page) {
    setBusy(true); setError('');
    try { setResult(await api(`/bookings?page=${next}&pageSize=10`)); setPage(next); } catch (e) { setError(e.message); } finally { setBusy(false); }
  }
  return <section className="results"><h2>Bookings</h2><button onClick={() => load(1)} disabled={busy}>{busy ? 'Loading…' : 'Load / refresh bookings'}</button><ErrorMessage message={error}/>
    {!result && <p>Load bookings to see your saved journeys.</p>}
    {result && <><p>{result.total} bookings · Page {page}</p>{!result.items.length && <p>No bookings yet. Search a flight to create one.</p>}
      {result.items.map(b => <article className="offer" key={b.reference}><div><span className={`badge status-${b.status.toLowerCase()}`}>{b.status}</span><h3>{b.origin} → {b.destination}</h3><p>{b.departureDate} · {b.passengers.length} passengers · {money(b.totalPrice)}</p><code>{b.reference}</code></div><button onClick={() => onOpen(b)}>View booking</button></article>)}
      <div className="actions"><button disabled={busy || page <= 1} onClick={() => load(page - 1)}>Previous</button><button disabled={busy || page * 10 >= result.total} onClick={() => load(page + 1)}>Next</button></div></>}
  </section>;
}

function BookingDetail({ initial, onBack }) {
  const [booking, setBooking] = useState(initial);
  const [email, setEmail] = useState(initial.contactEmail);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  async function run(operation) {
    setBusy(true); setError('');
    try { const updated = await operation(); setBooking(updated); setEmail(updated.contactEmail); }
    catch (e) { setError(e.message); } finally { setBusy(false); }
  }
  const actions = { Pending: ['confirm', 'cancel'], Confirmed: ['ticket', 'cancel'], Ticketed: ['cancel'], Cancelled: booking.paymentReference ? ['refund'] : [], Refunded: [] }[booking.status] || [];
  const labels = { confirm: 'Confirm with supplier', ticket: 'Mock pay & issue tickets', cancel: 'Cancel booking', refund: 'Mock refund' };
  return <section className="panel booking-detail-panel"><p className="eyebrow">BOOKING DETAILS</p><h2>{booking.origin} → {booking.destination}</h2><p className="reference">Reference: <strong>{booking.reference}</strong></p><span className={`badge status-${booking.status.toLowerCase()}`}>{booking.status}</span><ErrorMessage message={error}/>
    <div className="detail-grid"><div><p>{booking.airline} · {booking.departureDate}</p><p>Total: <strong>{money(booking.totalPrice)}</strong></p><h3>Passengers</h3>{booking.passengers.map(p => <p key={p.id}>{p.fullName}</p>)}</div>
      <div><h3>Supplier & payment</h3><p className="reference">{booking.supplierReference || 'Not confirmed yet'}</p><p className="reference">{booking.paymentReference || 'No mock payment yet'}</p><h3>Tickets</h3>{booking.tickets.length ? booking.tickets.map(t => <p className="reference" key={t.number}>{t.number}</p>) : <p>No tickets issued.</p>}</div></div>
    <form onSubmit={event => { event.preventDefault(); run(() => api(`/bookings/${booking.reference}/contact`, { method: 'PUT', body: { contactEmail: email, version: booking.version } })); }}>
      <fieldset disabled={busy || ['Cancelled', 'Refunded'].includes(booking.status)} className="search-grid"><label>Contact email<input required type="email" value={email} onChange={e => setEmail(e.target.value)}/></label><button>Update contact</button></fieldset></form>
    <div className="actions">{actions.map(action => <button key={action} disabled={busy} onClick={() => run(() => api(`/bookings/${booking.reference}/${action}`, { method: 'POST', body: { version: booking.version } }))}>{labels[action]}</button>)}
      <button className="secondary" disabled={busy} onClick={() => run(() => api(`/bookings/${booking.reference}`))}>Reload</button><button className="secondary" disabled={busy} onClick={onBack}>Back to bookings</button></div>
    <h3>Booking history</h3><ol className="history">{booking.events.map((event, i) => <li key={i}>{event.action} · {new Date(event.atUtc).toLocaleString()}</li>)}</ol>
  </section>;
}

function Admin() {
  const [users, setUsers] = useState(null);
  const [report, setReport] = useState(null);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  async function load() { setBusy(true); setError(''); try { setUsers(await api('/admin/users')); setReport(await api('/admin/reports')); } catch (e) { setError(e.message); } finally { setBusy(false); } }
  async function change(user, role) { setBusy(true); setError(''); try { await api(`/admin/users/${user.id}/role`, { method: 'PUT', body: { role } }); setUsers(await api('/admin/users')); } catch (e) { setError(e.message); } finally { setBusy(false); } }
  return <section className="panel admin-panel"><p className="eyebrow">INTERNAL OPERATIONS</p><h2>Admin overview</h2><button disabled={busy} onClick={load}>{busy ? 'Loading…' : 'Load / refresh dashboard'}</button><ErrorMessage message={error}/>
    {report && <><h3>Mock ticketed revenue: {money(report.ticketedRevenue)}</h3><div className="metrics">{report.groups.map(g => <article key={g.status}><strong>{g.count}</strong><span>{g.status}</span><p>{money(g.total)} booking value</p></article>)}</div></>}
    {users && <><h3>User & agent management</h3><p className="hint">Assign a role. Users should sign in again after changes.</p><div className="table-wrap"><table><thead><tr><th>Email</th><th>Role</th></tr></thead><tbody>{users.map(user => <tr key={user.id}><td>{user.email}</td><td><select disabled={busy} aria-label={`Role for ${user.email}`} value={user.roles[0] || 'Customer'} onChange={e => change(user, e.target.value)}>{['Customer', 'Agent', 'Admin'].map(role => <option key={role}>{role}</option>)}</select></td></tr>)}</tbody></table></div></>}
  </section>;
}
