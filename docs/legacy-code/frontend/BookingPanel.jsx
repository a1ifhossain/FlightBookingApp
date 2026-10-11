import { useState } from 'react';
import { createBooking, getBooking } from './api.js';

export default function BookingPanel({ offer, onClose }) {
  const [name, setName] = useState('');
  const [email, setEmail] = useState('');
  const [booking, setBooking] = useState(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');

  async function submit(event) {
    event.preventDefault();
    setBusy(true); setError('');
    try {
      const result = await createBooking({ offerId: offer.id, origin: offer.origin,
        destination: offer.destination, departureDate: offer.departureDate,
        passengerName: name.trim(), contactEmail: email.trim() });
      setBooking(result);
    } catch (error) { setError(error.message); }
    finally { setBusy(false); }
  }

  async function refresh() {
    setBusy(true); setError('');
    try { setBooking(await getBooking(booking.reference)); }
    catch (error) { setError(error.message); }
    finally { setBusy(false); }
  }

  return <section className="search-panel" aria-label="Booking details">
    <h2>{booking ? 'Booking created' : 'Passenger details'}</h2>
    <p>{offer.airline} · {offer.origin} → {offer.destination} · {offer.departureDate}</p>
    <p className="hint">Practice only: use fictional passenger details. Bookings disappear when the API restarts.</p>
    {error && <p className="error" role="alert">{error}</p>}
    {booking ? <div aria-live="polite"><p>Reference: <strong className="reference">{booking.reference}</strong></p>
      <p>Passenger: {booking.passengerName}</p><p>Status: <strong>{booking.status}</strong> — ticketing and payment are not implemented yet.</p>
      <p>Backend price: {booking.flight.price} {booking.flight.currency}</p>
      <button disabled={busy} onClick={refresh}>{busy ? 'Loading…' : 'Reload booking from API'}</button>
      <button className="secondary" disabled={busy} onClick={onClose}>Close</button></div>
      : <form onSubmit={submit}><fieldset disabled={busy}>
        <label>Passenger name<input required minLength={2} maxLength={100} value={name} onChange={event => setName(event.target.value)} placeholder="Demo Passenger"/></label>
        <label>Contact email<input required type="email" maxLength={254} value={email} onChange={event => setEmail(event.target.value)} placeholder="demo@example.com"/></label>
        <button>{busy ? 'Creating…' : 'Create mock booking'}</button>
        <button type="button" className="secondary" onClick={onClose}>Cancel</button>
      </fieldset></form>}
  </section>;
}

export function BookingLookup() {
  const [reference, setReference] = useState('');
  const [booking, setBooking] = useState(null);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  async function submit(event) {
    event.preventDefault(); setBusy(true); setError(''); setBooking(null);
    try { setBooking(await getBooking(reference.trim())); }
    catch (error) { setError(error.message); }
    finally { setBusy(false); }
  }
  return <section className="search-panel lookup"><h2>Find a mock booking</h2><form onSubmit={submit}>
    <fieldset disabled={busy}><label>Booking reference<input required maxLength={32} value={reference} onChange={event => setReference(event.target.value)} /></label>
    <button>{busy ? 'Loading…' : 'Find booking'}</button></fieldset></form>
    {error && <p className="error" role="alert">{error}</p>}
    {booking && <div aria-live="polite"><p>{booking.passengerName} · {booking.flight.origin} → {booking.flight.destination}</p><p>{booking.flight.departureDate} · {booking.flight.price} {booking.flight.currency} · {booking.status}</p></div>}
  </section>;
}
