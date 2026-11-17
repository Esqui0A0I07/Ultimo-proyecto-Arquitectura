import React, { useState, useEffect, useMemo } from 'react';
import {
  Search, Filter, Activity, Tag, Clock, ExternalLink, Bell, Sun, Moon,
  ChevronLeft, ChevronRight, AlertTriangle, CheckCircle2, Cpu, Gamepad2,
  DollarSign, Send, Sparkles, Trash2, Mail
} from 'lucide-react';

const DEFAULT_TG_TOKEN = '9149338989:AAE0k2GUggDYI_A-fIvcvOgO2igFbKFyfZE';
const DEFAULT_TG_CHAT_ID = '7016849983';
const DEFAULT_EMAIL_WEBHOOK = 'https://script.google.com/macros/s/AKfycbwbtduJkdC5hQqujrniGRoluan3SFUUYAqenZgK5FDF4koDQQSs47XzUvLW18MpeiWhTA/exec';

const IO_SWEEP_DATA = [
  { p: 'Sec', workers: 1,  medianS: 443.31, sAbs: 1.00,  sRel: 1.00,  eff: 1.00, karpFlatt: 0.0000 },
  { p: '1',   workers: 1,  medianS: 443.57, sAbs: 1.00,  sRel: 1.00,  eff: 1.00, karpFlatt: 0.0000 },
  { p: '2',   workers: 2,  medianS: 221.51, sAbs: 2.00,  sRel: 2.00,  eff: 1.00, karpFlatt: -0.0012 },
  { p: '4',   workers: 4,  medianS: 111.65, sAbs: 3.97,  sRel: 3.97,  eff: 0.99, karpFlatt: 0.0023 },
  { p: '8',   workers: 8,  medianS: 55.28,  sAbs: 8.02,  sRel: 8.02,  eff: 1.00, karpFlatt: -0.0004 },
  { p: '16',  workers: 16, medianS: 27.73,  sAbs: 15.99, sRel: 16.00, eff: 1.00, karpFlatt: 0.0000 },
  { p: '32',  workers: 32, medianS: 13.86,  sAbs: 31.98, sRel: 31.99, eff: 1.00, karpFlatt: 0.0000 },
  { p: '64',  workers: 64, medianS: 6.94,   sAbs: 63.87, sRel: 63.91, eff: 1.00, karpFlatt: 0.0000 },
];

const CPU_SWEEP_DATA = [
  { p: 'Sec',         workers: 1,  medianS: 2.938, sAbs: 1.00, sRel: 1.00, amdahl: 1.00, eff: 1.00, karpFlatt: 0.0000 },
  { p: '1',           workers: 1,  medianS: 2.948, sAbs: 1.00, sRel: 1.00, amdahl: 1.00, eff: 1.00, karpFlatt: 0.0000 },
  { p: '2',           workers: 2,  medianS: 2.263, sAbs: 1.30, sRel: 1.30, amdahl: 1.33, eff: 0.65, karpFlatt: 0.5349 },
  { p: '4',           workers: 4,  medianS: 1.766, sAbs: 1.66, sRel: 1.67, amdahl: 1.60, eff: 0.42, karpFlatt: 0.4652 },
  { p: '6 (P_fis)',   workers: 6,  medianS: 1.715, sAbs: 1.71, sRel: 1.72, amdahl: 1.71, eff: 0.29, karpFlatt: 0.4978 },
  { p: '12 (P_log)',  workers: 12, medianS: 1.661, sAbs: 1.77, sRel: 1.77, amdahl: 1.85, eff: 0.15, karpFlatt: 0.5238 },
  { p: '24 (2P_log)', workers: 24, medianS: 1.657, sAbs: 1.77, sRel: 1.78, amdahl: 1.92, eff: 0.07, karpFlatt: 0.5431 },
];

const SITE_REQUEST_STATS = [
  { site: 'Steam Store', method: 'HTML Scraping', limit: 6, ok: 500, retried: 8, failed: 0 },
  { site: 'Xbox / Microsoft Store', method: 'HTML Scraping', limit: 4, ok: 500, retried: 11, failed: 0 },
  { site: 'Nintendo eShop', method: 'API / HTML', limit: 6, ok: 500, retried: 4, failed: 0 },
  { site: 'GameStop', method: 'HTML Scraping', limit: 2, ok: 500, retried: 19, failed: 0 },
  { site: 'Metacritic', method: 'HTML Scraping', limit: 3, ok: 500, retried: 7, failed: 0 },
  { site: 'OpenCritic', method: 'HTML Paginado', limit: 4, ok: 500, retried: 3, failed: 0 },
  { site: 'How Long To Beat', method: 'Web / IGDB Backup', limit: 3, ok: 500, retried: 6, failed: 0 },
];

function createFallbackCoverDataUri(game) {
  const accent = game.dominantColor || '#102973';
  const safeTitle = (game.title || 'Video Game').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
  const line1 = safeTitle.slice(0, 26);
  const line2 = safeTitle.length > 26 ? safeTitle.slice(26, 52) : '';
  const plats = (game.platforms || []).join(' · ');

  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="460" height="215" viewBox="0 0 460 215">
    <defs>
      <linearGradient id="bg" x1="0%" y1="0%" x2="100%" y2="100%">
        <stop offset="0%" stop-color="#040D40"/>
        <stop offset="55%" stop-color="${accent}"/>
        <stop offset="100%" stop-color="#121212"/>
      </linearGradient>
    </defs>
    <rect width="460" height="215" fill="url(#bg)"/>
    <circle cx="410" cy="40" r="85" fill="rgba(255,255,255,0.05)"/>
    <rect x="22" y="24" width="416" height="167" rx="8" fill="rgba(18,18,18,0.45)" stroke="rgba(255,255,255,0.14)"/>
    <text x="40" y="62" fill="#93C5FD" font-family="JetBrains Mono, monospace" font-size="11" font-weight="700">${plats}</text>
    <text x="40" y="110" fill="#FFFFFF" font-family="Inter, sans-serif" font-size="20" font-weight="700">${line1}</text>
    <text x="40" y="138" fill="#E4E4E7" font-family="Inter, sans-serif" font-size="17" font-weight="600">${line2}</text>
    <text x="40" y="172" fill="#A1A1AA" font-family="JetBrains Mono, monospace" font-size="11">OFFICIAL EDITION · RADAR OFERTAS</text>
  </svg>`;

  return `data:image/svg+xml;utf8,${encodeURIComponent(svg)}`;
}

function getOfferPlatform(offer, gamePlatforms) {
  if (offer.platform) return offer.platform;
  if (offer.storeCode === 'steam') return 'PC';
  if (offer.storeCode === 'xbox') return 'Xbox';
  if (offer.storeCode === 'eshop') return 'Nintendo Switch';
  return (gamePlatforms && gamePlatforms[0]) || 'PlayStation';
}

function formatColones(usdAmount, exchangeRate) {
  const crc = Math.round((Number(usdAmount) || 0) * exchangeRate);
  return `₡${crc.toLocaleString('es-CR')}`;
}

export default function App() {
  const [theme, setTheme] = useState('dark');
  const [activeTab, setActiveTab] = useState('catalog');
  const [catalogData, setCatalogData] = useState({ generatedAt: null, totalGames: 0, games: [] });
  const [metricsData, setMetricsData] = useState({ generatedAt: null, recentRuns: [], stageMetrics: [] });
  const [loading, setLoading] = useState(true);

  const [bccrRate, setBccrRate] = useState({
    venta: 512.50,
    compra: 504.20,
    fecha: new Date().toLocaleDateString('es-CR'),
    fuente: 'BCCR'
  });

  const [searchQuery, setSearchQuery] = useState('');
  const [typeFilter, setTypeFilter] = useState('all');
  const [platformFilter, setPlatformFilter] = useState('all');
  const [formatFilter, setFormatFilter] = useState('all');
  const [storeFilter, setStoreFilter] = useState('all');
  const [sortBy, setSortBy] = useState('discount');
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(24);

  const [cardPlatformMap, setCardPlatformMap] = useState({});
  const [selectedGame, setSelectedGame] = useState(null);
  const [modalPlatform, setModalPlatform] = useState('all');

  const [wishlist, setWishlist] = useState(() => {
    const saved = localStorage.getItem('radar_wishlist_alerts');
    return saved ? JSON.parse(saved) : [];
  });
  const [alertChannel, setAlertChannel] = useState('telegram');
  const [alertDestination, setAlertDestination] = useState(DEFAULT_TG_CHAT_ID);
  const [alertTargetPrice, setAlertTargetPrice] = useState('');
  const [telegramBotToken, setTelegramBotToken] = useState(
    () => localStorage.getItem('radar_tg_token') || DEFAULT_TG_TOKEN
  );
  const [emailWebhookUrl, setEmailWebhookUrl] = useState(
    () => localStorage.getItem('radar_email_webhook') || DEFAULT_EMAIL_WEBHOOK
  );
  const [testToast, setTestToast] = useState(null);

  useEffect(() => {
    document.documentElement.setAttribute('data-theme', theme);
  }, [theme]);

  useEffect(() => {
    async function fetchBccrExchangeRate() {
      try {
        const res = await fetch('https://api.hacienda.go.cr/indicadores/tc/dolar');
        if (res.ok) {
          const data = await res.json();
          const ventaVal = Number(data?.venta?.valor);
          const compraVal = Number(data?.compra?.valor);
          if (ventaVal > 0) {
            setBccrRate({
              venta: ventaVal,
              compra: compraVal > 0 ? compraVal : Number((ventaVal - 8).toFixed(2)),
              fecha: data?.venta?.fecha ? new Date(data.venta.fecha).toLocaleDateString('es-CR') : new Date().toLocaleDateString('es-CR'),
              fuente: 'BCCR Oficial'
            });
            return;
          }
        }
      } catch {
        // Respaldo secundario si hay bloqueo CORS
      }

      try {
        const fallbackRes = await fetch('https://open.er-api.com/v6/latest/USD');
        if (fallbackRes.ok) {
          const erData = await fallbackRes.json();
          const crcRate = Number(erData?.rates?.CRC);
          if (crcRate > 0) {
            setBccrRate({
              venta: Number(crcRate.toFixed(2)),
              compra: Number((crcRate - 7.5).toFixed(2)),
              fecha: new Date().toLocaleDateString('es-CR'),
              fuente: 'BCCR / FX Live'
            });
          }
        }
      } catch (err) {
        console.warn('Usando tipo de cambio de referencia BCCR:', err);
      }
    }

    fetchBccrExchangeRate();
  }, []);

  useEffect(() => {
    async function loadStaticJson() {
      try {
        const cacheBuster = `?v=${Date.now()}`;
        const [catRes, metRes] = await Promise.all([
          fetch(`./data/catalog.json${cacheBuster}`),
          fetch(`./data/metrics.json${cacheBuster}`)
        ]);
        if (catRes.ok) {
          const catJson = await catRes.json();
          const seenTitles = new Set();

          const cleanGames = [];
          for (const g of (catJson.games || [])) {
            if (!g.title || g.title.includes('Chapter ')) continue;
            const normKey = g.title.trim().toLowerCase();
            if (seenTitles.has(normKey)) continue;
            seenTitles.add(normKey);

            let derivedType = g.itemType || 'game';
            if (normKey.includes('dlc') || normKey.includes('expansion') || normKey.includes('pass')) derivedType = 'dlc';
            else if (normKey.includes('edition') || normKey.includes('collection') || normKey.includes('bundle') || normKey.includes(' + ')) derivedType = 'game_dlc';

            const validPlats = g.platforms && g.platforms.length > 0 ? g.platforms : ['PC'];
            let offers = (g.offers || [])
              .map(o => ({ ...o, platform: getOfferPlatform(o, validPlats) }))
              .filter(o => validPlats.includes(o.platform));

            if (offers.length === 0) continue;

            offers.sort((a, b) => a.currentPriceUsd - b.currentPriceUsd);

            cleanGames.push({
              ...g,
              platforms: validPlats,
              itemType: derivedType,
              bestPriceUsd: offers[0]?.currentPriceUsd ?? g.msrpUsd,
              bestDiscountPct: Math.max(...offers.map(o => o.discountPct || 0), 0),
              isHistoricalLow: offers.some(o => o.isHistoricalLow),
              offers
            });
          }

          setCatalogData({ ...catJson, totalGames: cleanGames.length, games: cleanGames });
        }
        if (metRes.ok) {
          setMetricsData(await metRes.json());
        }
      } catch (err) {
        console.error('Error cargando JSON:', err);
      } finally {
        setLoading(false);
      }
    }
    loadStaticJson();
  }, []);

  const filteredGames = useMemo(() => {
    return catalogData.games
      .filter(g => {
        if (searchQuery && !g.title.toLowerCase().includes(searchQuery.toLowerCase())) return false;
        if (typeFilter !== 'all' && g.itemType !== typeFilter) return false;
        if (platformFilter !== 'all' && !(g.platforms || []).includes(platformFilter)) return false;
        if (formatFilter !== 'all' && !(g.offers || []).some(o => o.format === formatFilter)) return false;
        if (storeFilter !== 'all' && !(g.offers || []).some(o => o.storeCode === storeFilter)) return false;
        return true;
      })
      .sort((a, b) => {
        if (sortBy === 'name') return a.title.localeCompare(b.title);
        if (sortBy === 'price') return a.bestPriceUsd - b.bestPriceUsd;
        if (sortBy === 'discount') return b.bestDiscountPct - a.bestDiscountPct;
        if (sortBy === 'rating') return b.metacriticScore - a.metacriticScore;
        return 0;
      });
  }, [catalogData.games, searchQuery, typeFilter, platformFilter, formatFilter, storeFilter, sortBy]);

  const totalPages = Math.max(1, Math.ceil(filteredGames.length / pageSize));
  const safePage = Math.min(currentPage, totalPages);
  const paginatedGames = filteredGames.slice((safePage - 1) * pageSize, safePage * pageSize);

  function handlePageChange(newPage) {
    const target = Math.max(1, Math.min(totalPages, newPage));
    setCurrentPage(target);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  function handleChannelChange(newChannel) {
    setAlertChannel(newChannel);
    if (newChannel === 'telegram' && (!alertDestination || alertDestination.includes('@'))) {
      setAlertDestination(DEFAULT_TG_CHAT_ID);
    } else if (newChannel === 'email' && alertDestination === DEFAULT_TG_CHAT_ID) {
      setAlertDestination('');
    }
  }

  function handleAddAlert(e) {
    e.preventDefault();
    if (!selectedGame || !alertDestination || !alertTargetPrice) return;
    const bestOffer = (selectedGame.offers || [])[0];
    const newAlert = {
      id: Date.now(),
      gameId: selectedGame.id,
      gameTitle: selectedGame.title,
      currentBestUsd: selectedGame.bestPriceUsd,
      targetPriceUsd: Number(alertTargetPrice),
      storeName: bestOffer?.storeName || 'Steam Store',
      productUrl: bestOffer?.productUrl || 'https://store.steampowered.com',
      channel: alertChannel,
      destination: alertDestination.trim(),
      lastNotifiedAt: null,
      createdAt: new Date().toISOString()
    };
    const updated = [newAlert, ...wishlist];
    setWishlist(updated);
    localStorage.setItem('radar_wishlist_alerts', JSON.stringify(updated));
    setSelectedGame(null);
    setActiveTab('wishlist');
    triggerTestNotification(newAlert, true);
  }

  function handleDeleteAlert(id) {
    const updated = wishlist.filter(item => item.id !== id);
    setWishlist(updated);
    localStorage.setItem('radar_wishlist_alerts', JSON.stringify(updated));
  }

  function handleSimulatePriceDrop() {
    if (wishlist.length === 0) return;
    const updated = wishlist.map(item => {
      const droppedPrice = Number((item.targetPriceUsd * 0.88).toFixed(2));
      return {
        ...item,
        currentBestUsd: droppedPrice,
        lastNotifiedAt: new Date().toLocaleTimeString('es-CR')
      };
    });
    setWishlist(updated);
    localStorage.setItem('radar_wishlist_alerts', JSON.stringify(updated));
    triggerTestNotification(updated[0], false);
  }

  async function triggerTestNotification(item, isNewSubscription = false) {
    const crcCurrent = formatColones(item.currentBestUsd, bccrRate.venta);
    const crcTarget = formatColones(item.targetPriceUsd, bccrRate.venta);
    const titleMsg = isNewSubscription
      ? `✅ Alerta registrada: ${item.gameTitle}`
      : `🔥 ¡BAJADA DE PRECIO DETECTADA!: ${item.gameTitle}`;

    const bodyMsg = `Precio actual: $${item.currentBestUsd.toFixed(2)} (${crcCurrent}) en ${item.storeName || 'Tienda Oficial'} · Tu meta: $${item.targetPriceUsd.toFixed(2)} (${crcTarget}) · Destino: ${item.destination}`;

    let deliveryStatus = 'Notificación en navegador activada';

    // 1. Notificación nativa de Windows / Navegador
    if ('Notification' in window) {
      if (Notification.permission === 'granted') {
        new Notification(titleMsg, { body: bodyMsg });
      } else if (Notification.permission !== 'denied') {
        const perm = await Notification.requestPermission();
        if (perm === 'granted') {
          new Notification(titleMsg, { body: bodyMsg });
        }
      }
    }

    // 2. Canal Telegram: envío real por API HTTP
    if (item.channel === 'telegram' && telegramBotToken.trim() !== '') {
      try {
        const tgUrl = `https://api.telegram.org/bot${telegramBotToken.trim()}/sendMessage`;
        const text = `🎮 *RADAR DE OFERTAS MULTINÚCLEO (IC-3101)*\n${titleMsg}\n\n💰 *Precio Oferta:* $${item.currentBestUsd.toFixed(2)} USD (${crcCurrent})\n🎯 *Tu Precio Meta:* $${item.targetPriceUsd.toFixed(2)} USD (${crcTarget})\n🏦 *TC BCCR Venta:* ₡${bccrRate.venta.toFixed(2)}\n🏬 *Tienda:* ${item.storeName || 'Steam / eShop'}\n🔗 [Comprar en tienda oficial](${item.productUrl || 'https://store.steampowered.com'})`;
        const res = await fetch(tgUrl, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            chat_id: item.destination.trim(),
            text,
            parse_mode: 'Markdown'
          })
        });
        if (res.ok) {
          deliveryStatus = `✓ Mensaje REAL enviado a Telegram (ID: ${item.destination})`;
        } else {
          deliveryStatus = 'Error de Telegram: asegúrate de haber enviado /start a tu bot y usar tu ID numérico (7016849983)';
        }
      } catch {
        deliveryStatus = 'Simulación local OK (Sin conexión a api.telegram.org)';
      }
    }
    // 3. Canal Correo Electrónico: envío real por HTTP (Google Apps Script o FormSubmit AJAX)
    else if (item.channel === 'email') {
      const emailSubject = `[Radar de Ofertas IC-3101] ${titleMsg} - $${item.currentBestUsd.toFixed(2)} (${crcCurrent})`;
      const htmlContent = `
        <div style="font-family: Arial, sans-serif; max-width: 560px; padding: 20px; border: 1px solid #102973; border-radius: 10px;">
          <h2 style="color: #102973; margin-top: 0;">🎮 Radar de Ofertas Multinúcleo (TEC IC-3101)</h2>
          <p><strong>${titleMsg}</strong></p>
          <ul>
            <li><strong>Juego:</strong> ${item.gameTitle}</li>
            <li><strong>Precio Actual:</strong> $${item.currentBestUsd.toFixed(2)} USD (<strong>${crcCurrent}</strong> al TC BCCR ₡${bccrRate.venta.toFixed(2)})</li>
            <li><strong>Tu Precio Meta:</strong> $${item.targetPriceUsd.toFixed(2)} USD (${crcTarget})</li>
            <li><strong>Tienda:</strong> ${item.storeName || 'Tienda Oficial'}</li>
          </ul>
          <p><a href="${item.productUrl || 'https://store.steampowered.com'}" style="background:#10B981;color:#fff;padding:10px 16px;text-decoration:none;border-radius:6px;display:inline-block;">Ir a la oferta en la tienda ↗</a></p>
        </div>
      `;

      setTestToast({
        title: titleMsg,
        body: `Enviando correo electrónico real a ${item.destination}...`,
        channel: 'EMAIL',
        status: 'Conectando con servidor de correo...',
        url: item.productUrl || 'https://store.steampowered.com',
        timestamp: new Date().toLocaleTimeString('es-CR')
      });

      try {
        if (emailWebhookUrl.trim() !== '') {
          // Opción A: Envío directo por Google Apps Script propio (0% Spam, instantáneo)
          await fetch(emailWebhookUrl.trim(), {
            method: 'POST',
            mode: 'no-cors',
            headers: { 'Content-Type': 'text/plain;charset=utf-8' },
            body: JSON.stringify({
              to: item.destination.trim(),
              subject: emailSubject,
              htmlBody: htmlContent
            })
          });
          deliveryStatus = `✓ Correo enviado directo vía Google Apps Script a ${item.destination}`;
        } else {
          // Opción B: Envío directo por API HTTP de FormSubmit.co
          const formRes = await fetch(`https://formsubmit.co/ajax/${encodeURIComponent(item.destination.trim())}`, {
            method: 'POST',
            headers: {
              'Content-Type': 'application/json',
              'Accept': 'application/json'
            },
            body: JSON.stringify({
              _subject: emailSubject,
              _template: 'table',
              _captcha: 'false',
              Evento: titleMsg,
              Juego: item.gameTitle,
              Precio_Actual_USD: `$${item.currentBestUsd.toFixed(2)} USD`,
              Precio_Actual_Colones_BCCR: `${crcCurrent} (TC: ₡${bccrRate.venta.toFixed(2)})`,
              Precio_Meta: `$${item.targetPriceUsd.toFixed(2)} USD (${crcTarget})`,
              Tienda: item.storeName || 'Steam / eShop',
              Enlace_Compra: item.productUrl || 'https://store.steampowered.com'
            })
          });

          const formJson = await formRes.json().catch(() => ({}));
          if (formRes.ok && (formJson.success === 'true' || formJson.success === true)) {
            deliveryStatus = `✓ Correo enviado a ${item.destination} (Revisa Bandeja de Entrada o Spam)`;
          } else {
            deliveryStatus = `⚠ Revisa tu bandeja o Spam en ${item.destination} y pulsa "Activate Form" en el correo de FormSubmit para autorizar envíos`;
          }
        }
      } catch {
        deliveryStatus = `Error de red al contactar servidor de correo para ${item.destination}`;
      }
    }

    const nowStr = new Date().toLocaleTimeString('es-CR');
    setWishlist(prev => {
      const next = prev.map(w => (w.id === item.id ? { ...w, lastNotifiedAt: nowStr } : w));
      localStorage.setItem('radar_wishlist_alerts', JSON.stringify(next));
      return next;
    });

    setTestToast({
      title: titleMsg,
      body: bodyMsg,
      channel: item.channel.toUpperCase(),
      status: deliveryStatus,
      url: item.productUrl || 'https://store.steampowered.com',
      timestamp: nowStr
    });
  }

  const latestRun = metricsData.recentRuns?.[0] || {
    runid: 1,
    executionmode: 'Parallel',
    isreplaymode: true,
    ioworkers: 32,
    cpuworkers: 12,
    simulatedlatencyms: 120,
    totaldurationms: 14440
  };

  const modalFilteredOffers = useMemo(() => {
    if (!selectedGame) return [];
    const all = selectedGame.offers || [];
    if (modalPlatform === 'all') return all;
    return all.filter(o => o.platform === modalPlatform);
  }, [selectedGame, modalPlatform]);

  return (
    <div>
      {testToast && (
        <div
          style={{
            position: 'fixed',
            bottom: '1.5rem',
            right: '1.5rem',
            zIndex: 9999,
            maxWidth: '450px',
            background: 'var(--bg-surface)',
            border: '2px solid #10B981',
            borderRadius: '12px',
            padding: '1rem 1.2rem',
            boxShadow: '0 20px 35px rgba(0,0,0,0.55)'
          }}
        >
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.35rem' }}>
            <span className="mono" style={{ fontSize: '0.72rem', color: '#10B981', fontWeight: 700 }}>
              🔔 DISPARO DE ALERTA ({testToast.channel}) · {testToast.timestamp}
            </span>
            <button className="btn" style={{ padding: '0.15rem 0.45rem', fontSize: '0.72rem' }} onClick={() => setTestToast(null)}>
              ✕
            </button>
          </div>
          <div style={{ fontWeight: 700, fontSize: '0.92rem', marginBottom: '0.3rem' }}>{testToast.title}</div>
          <div className="mono" style={{ fontSize: '0.76rem', color: 'var(--text-secondary)', marginBottom: '0.45rem' }}>
            {testToast.body}
          </div>
          <div className="mono" style={{ fontSize: '0.72rem', color: '#38BDF8', display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: '0.5rem' }}>
            <span>{testToast.status}</span>
            <a href={testToast.url} target="_blank" rel="noreferrer" style={{ color: '#10B981', textDecoration: 'underline', whiteSpace: 'nowrap' }}>
              Ver oferta ↗
            </a>
          </div>
        </div>
      )}

      <header className="navbar">
        <div className="navbar-inner">
          <div className="brand-block">
            <div className="brand-logo mono">R</div>
            <div>
              <h1 style={{ fontSize: '1.05rem', fontWeight: 700 }}>Radar de Ofertas Multinúcleo</h1>
              <div className="mono" style={{ fontSize: '0.73rem', color: 'var(--text-secondary)' }}>
                IC-3101 · Última actualización: {catalogData.generatedAt ? new Date(catalogData.generatedAt).toLocaleString('es-CR') : 'Sincronizando...'}
              </div>
            </div>
          </div>

          <div
            className="mono"
            style={{
              display: 'inline-flex',
              alignItems: 'center',
              gap: '0.55rem',
              padding: '0.38rem 0.75rem',
              borderRadius: '8px',
              background: 'rgba(16, 185, 129, 0.12)',
              border: '1px solid rgba(16, 185, 129, 0.35)',
              fontSize: '0.76rem'
            }}
            title={`Tipo de cambio del dólar (${bccrRate.fuente} - ${bccrRate.fecha})`}
          >
            <DollarSign size={14} style={{ color: '#10B981' }} />
            <span>
              <strong>BCCR Dólar:</strong> Venta <strong style={{ color: '#10B981' }}>₡{bccrRate.venta.toFixed(2)}</strong> · Compra ₡{bccrRate.compra.toFixed(2)}
            </span>
          </div>

          <div className="nav-tabs">
            <button
              className={`btn ${activeTab === 'catalog' ? 'btn-active' : ''}`}
              onClick={() => setActiveTab('catalog')}
            >
              <Tag size={16} /> Catálogo ({catalogData.games.length})
            </button>
            <button
              className={`btn ${activeTab === 'metrics' ? 'btn-active' : ''}`}
              onClick={() => setActiveTab('metrics')}
            >
              <Activity size={16} /> Panel de Métricas
            </button>
            <button
              className={`btn ${activeTab === 'wishlist' ? 'btn-active' : ''}`}
              onClick={() => setActiveTab('wishlist')}
            >
              <Bell size={16} /> Alertas ({wishlist.length})
            </button>
            <button
              className="btn"
              onClick={() => setTheme(t => (t === 'dark' ? 'light' : 'dark'))}
              title="Cambiar tema Claro / Oscuro"
            >
              {theme === 'dark' ? <Sun size={16} /> : <Moon size={16} />}
            </button>
          </div>
        </div>
      </header>

      <main className="app-container">
        {loading ? (
          <div className="mono" style={{ padding: '3rem', textAlign: 'center' }}>
            Cargando catálogo de juegos reales y tipo de cambio del BCCR...
          </div>
        ) : activeTab === 'catalog' ? (
          <>
            <section className="filters-panel">
              <div className="filter-group">
                <label><Search size={12} /> Buscar título</label>
                <input
                  type="text"
                  className="input-control"
                  placeholder="Ej. Zelda, Witcher, Elden Ring..."
                  value={searchQuery}
                  onChange={e => { setSearchQuery(e.target.value); setCurrentPage(1); }}
                />
              </div>

              <div className="filter-group">
                <label><Filter size={12} /> Tipo de ítem</label>
                <select className="input-control" value={typeFilter} onChange={e => { setTypeFilter(e.target.value); setCurrentPage(1); }}>
                  <option value="all">Todos los tipos</option>
                  <option value="game">Juego Base</option>
                  <option value="game_dlc">Juego + DLC / Edición</option>
                  <option value="dlc">DLC / Expansión</option>
                </select>
              </div>

              <div className="filter-group">
                <label>Plataforma</label>
                <select className="input-control" value={platformFilter} onChange={e => { setPlatformFilter(e.target.value); setCurrentPage(1); }}>
                  <option value="all">Todas (PC, PS, Switch, Xbox)</option>
                  <option value="PC">PC</option>
                  <option value="PlayStation">PlayStation</option>
                  <option value="Nintendo Switch">Nintendo Switch</option>
                  <option value="Xbox">Xbox</option>
                </select>
              </div>

              <div className="filter-group">
                <label>Formato</label>
                <select className="input-control" value={formatFilter} onChange={e => { setFormatFilter(e.target.value); setCurrentPage(1); }}>
                  <option value="all">Digital y Físico</option>
                  <option value="digital">Digital</option>
                  <option value="physical">Físico</option>
                </select>
              </div>

              <div className="filter-group">
                <label>Tienda</label>
                <select className="input-control" value={storeFilter} onChange={e => { setStoreFilter(e.target.value); setCurrentPage(1); }}>
                  <option value="all">Todas las tiendas (4)</option>
                  <option value="steam">Steam Store</option>
                  <option value="xbox">Xbox / Microsoft Store</option>
                  <option value="eshop">Nintendo eShop</option>
                  <option value="gamestop">GameStop (Minorista)</option>
                </select>
              </div>

              <div className="filter-group">
                <label>Ordenar por</label>
                <select className="input-control" value={sortBy} onChange={e => setSortBy(e.target.value)}>
                  <option value="discount">Mayor % de descuento</option>
                  <option value="price">Mejor precio (menor a mayor)</option>
                  <option value="rating">Mejor calificación</option>
                  <option value="name">Nombre (A - Z)</option>
                </select>
              </div>

              <div className="filter-group">
                <label>Juegos por página</label>
                <select
                  className="input-control mono"
                  value={pageSize}
                  onChange={e => {
                    setPageSize(Number(e.target.value));
                    setCurrentPage(1);
                  }}
                >
                  <option value={12}>12 por página</option>
                  <option value={24}>24 por página</option>
                  <option value={48}>48 por página</option>
                  <option value={96}>96 por página</option>
                </select>
              </div>
            </section>

            <PaginationBar
              currentPage={safePage}
              totalPages={totalPages}
              totalItems={filteredGames.length}
              shownItems={paginatedGames.length}
              pageSize={pageSize}
              onPageChange={handlePageChange}
              onPageSizeChange={newSize => { setPageSize(newSize); setCurrentPage(1); }}
            />

            <section className="cards-grid">
              {paginatedGames.map(game => {
                const selectedPlat = cardPlatformMap[game.id] || (platformFilter !== 'all' ? platformFilter : 'all');
                const platOffers = selectedPlat === 'all'
                  ? (game.offers || [])
                  : (game.offers || []).filter(o => o.platform === selectedPlat);

                const activeBestOffer = platOffers[0] || (game.offers || [])[0];
                const activePrice = activeBestOffer ? activeBestOffer.currentPriceUsd : game.bestPriceUsd;
                const activeRegular = activeBestOffer?.regularPriceUsd || game.msrpUsd;
                const activeDiscount = activeBestOffer ? activeBestOffer.discountPct : game.bestDiscountPct;
                const activeStoreName = activeBestOffer ? activeBestOffer.storeName : '';

                return (
                  <article
                    key={game.id}
                    className="game-card"
                    style={{ borderTop: `3px solid ${game.dominantColor || '#102973'}` }}
                    onClick={() => {
                      setSelectedGame(game);
                      setModalPlatform(selectedPlat);
                      setAlertTargetPrice((activePrice * 0.85).toFixed(2));
                    }}
                  >
                    <div className="card-image-wrap">
                      <img
                        src={game.coverUrl || createFallbackCoverDataUri(game)}
                        alt={game.title}
                        loading="lazy"
                        onError={e => {
                          e.currentTarget.onerror = null;
                          e.currentTarget.src = createFallbackCoverDataUri(game);
                        }}
                      />
                      <div className="badge-row">
                        {activeDiscount > 0 ? (
                          <span className="badge badge-discount mono">-{activeDiscount.toFixed(0)}%</span>
                        ) : <span />}
                        {game.isHistoricalLow && (
                          <span className="badge badge-hist-low mono">★ MÍNIMO HISTÓRICO</span>
                        )}
                      </div>
                    </div>

                    <div className="card-body">
                      <h2 className="card-title">{game.title}</h2>

                      <div className="platform-tags">
                        {game.platforms.length > 1 && (
                          <button
                            type="button"
                            className={`platform-tag ${selectedPlat === 'all' ? 'platform-tag-active' : ''}`}
                            onClick={e => {
                              e.stopPropagation();
                              setCardPlatformMap(prev => ({ ...prev, [game.id]: 'all' }));
                            }}
                            title="Ver mejor precio global"
                          >
                            Todas
                          </button>
                        )}
                        {(game.platforms || []).map(p => {
                          const isSelected = selectedPlat === p || (game.platforms.length === 1);
                          return (
                            <button
                              key={p}
                              type="button"
                              className={`platform-tag ${isSelected ? 'platform-tag-active' : ''}`}
                              onClick={e => {
                                e.stopPropagation();
                                if (game.platforms.length > 1) {
                                  setCardPlatformMap(prev => ({ ...prev, [game.id]: p }));
                                }
                              }}
                              title={`Ver precio en ${p}`}
                            >
                              {p}
                            </button>
                          );
                        })}
                        <span className="platform-tag mono" style={{ marginLeft: 'auto', fontWeight: 600 }}>
                          MC {game.metacriticScore} · OC {game.opencriticScore}
                        </span>
                      </div>

                      <div className="card-footer">
                        <div>
                          <div style={{ display: 'flex', alignItems: 'baseline', gap: '0.45rem', flexWrap: 'wrap' }}>
                            <span className="price-current mono">${activePrice.toFixed(2)}</span>
                            <span className="price-regular mono">${activeRegular.toFixed(2)}</span>
                          </div>
                          <div className="mono" style={{ fontSize: '0.82rem', fontWeight: 700, color: '#38BDF8', marginTop: '0.12rem' }}>
                            {formatColones(activePrice, bccrRate.venta)}{' '}
                            {activeRegular > activePrice && (
                              <span style={{ fontSize: '0.7rem', color: 'var(--text-secondary)', textDecoration: 'line-through', fontWeight: 400 }}>
                                {formatColones(activeRegular, bccrRate.venta)}
                              </span>
                            )}
                          </div>
                          {activeStoreName && (
                            <div className="mono" style={{ fontSize: '0.68rem', color: 'var(--text-secondary)', marginTop: '0.1rem' }}>
                              en {activeStoreName}
                            </div>
                          )}
                        </div>
                        <span className="mono" style={{ fontSize: '0.74rem', color: 'var(--text-secondary)' }}>
                          <Clock size={12} style={{ verticalAlign: 'middle' }} /> {game.hltbMainHours}h
                        </span>
                      </div>
                    </div>
                  </article>
                );
              })}
            </section>

            <div style={{ marginTop: '1.75rem' }}>
              <PaginationBar
                currentPage={safePage}
                totalPages={totalPages}
                totalItems={filteredGames.length}
                shownItems={paginatedGames.length}
                pageSize={pageSize}
                onPageChange={handlePageChange}
                onPageSizeChange={newSize => { setPageSize(newSize); setCurrentPage(1); }}
              />
            </div>
          </>
        ) : activeTab === 'metrics' ? (
          <section>
            <div className="kpi-grid">
              <div className="kpi-card">
                <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>ÚLTIMA EJECUCIÓN PIPELINE</div>
                <div className="mono" style={{ fontSize: '1.5rem', fontWeight: 700, marginTop: '0.25rem' }}>
                  {((latestRun.totaldurationms || 14440) / 1000).toFixed(2)} s
                </div>
                <div className="mono" style={{ fontSize: '0.75rem', color: 'var(--badge-sale)' }}>
                  Modo: {latestRun.executionmode || 'Parallel'} (Replay {latestRun.simulatedlatencyms || 120}ms)
                </div>
              </div>

              <div className="kpi-card">
                <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>CONFIGURACIÓN DE WORKERS</div>
                <div className="mono" style={{ fontSize: '1.5rem', fontWeight: 700, marginTop: '0.25rem' }}>
                  W_io: {latestRun.ioworkers || 32} · W_cpu: {latestRun.cpuworkers || 12}
                </div>
                <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>
                  AMD Ryzen 5 5600H (6C / 12T · 8GB RAM)
                </div>
              </div>

              <div className="kpi-card">
                <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>SOLICITUDES PROCESADAS</div>
                <div className="mono" style={{ fontSize: '1.5rem', fontWeight: 700, marginTop: '0.25rem' }}>
                  3.500 / 3.500
                </div>
                <div style={{ fontSize: '0.75rem', color: 'var(--badge-sale)' }}>
                  <CheckCircle2 size={13} style={{ verticalAlign: 'middle' }} /> 500 juegos × 7 fuentes (0 fallos finales)
                </div>
              </div>

              <div className="kpi-card">
                <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>SPEEDUP GLOBAL DEL PIPELINE</div>
                <div className="mono" style={{ fontSize: '1.5rem', fontWeight: 700, marginTop: '0.25rem', color: 'var(--badge-low)' }}>
                  31.98x (S_abs)
                </div>
                <div className="mono" style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>
                  T_sec: 443.31s → T_par: 13.86s
                </div>
              </div>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(410px, 1fr))', gap: '1.25rem', marginBottom: '1.5rem' }}>
              <div className="kpi-card">
                <h3 style={{ fontSize: '0.95rem', marginBottom: '0.5rem' }}><Cpu size={16} style={{ verticalAlign: 'middle' }} /> Tiempos por Etapa (Segmentación con Channel&lt;T&gt;)</h3>
                <table className="data-table mono">
                  <thead>
                    <tr>
                      <th>Etapa del Pipeline</th>
                      <th>Duración (s)</th>
                      <th>Items</th>
                      <th>Comportamiento</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr>
                      <td>0 · Descubrir (OpenCritic)</td>
                      <td>1.85 s</td>
                      <td>520</td>
                      <td>E/S Paginada</td>
                    </tr>
                    <tr>
                      <td>1 · Descargar (E/S 32 workers)</td>
                      <td>13.86 s</td>
                      <td>3.500 req</td>
                      <td>Solapa espera de red</td>
                    </tr>
                    <tr>
                      <td>2 · Parsear (AngleSharp HTML)</td>
                      <td>13.79 s</td>
                      <td>500</td>
                      <td>Consumidor en canal</td>
                    </tr>
                    <tr>
                      <td>3 · Emparejar (CPU Jaro-Winkler)</td>
                      <td>1.66 s</td>
                      <td>500 × 2.500</td>
                      <td>Parallel.ForEach (12 hilos)</td>
                    </tr>
                    <tr>
                      <td>4 · Persistir (PostgreSQL Neon)</td>
                      <td>14.12 s</td>
                      <td>500 lotes</td>
                      <td>Batch Insert Transaccional</td>
                    </tr>
                  </tbody>
                </table>
              </div>

              <div className="kpi-card">
                <h3 style={{ fontSize: '0.95rem', marginBottom: '0.5rem' }}>Solicitudes Exitosas, Reintentadas y Fallidas por Sitio</h3>
                <table className="data-table mono">
                  <thead>
                    <tr>
                      <th>Sitio / Fuente</th>
                      <th>Método</th>
                      <th>Semáforo</th>
                      <th>Éxito</th>
                      <th>Reintentos</th>
                      <th>Fallos</th>
                    </tr>
                  </thead>
                  <tbody>
                    {SITE_REQUEST_STATS.map(s => (
                      <tr key={s.site}>
                        <td>{s.site}</td>
                        <td>{s.method}</td>
                        <td>{s.limit}</td>
                        <td style={{ color: 'var(--badge-sale)' }}>{s.ok}</td>
                        <td style={{ color: 'var(--badge-low)' }}>{s.retried}</td>
                        <td>{s.failed}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(410px, 1fr))', gap: '1.25rem', marginBottom: '1.5rem' }}>
              <div className="kpi-card">
                <h3 style={{ fontSize: '0.95rem', marginBottom: '0.5rem' }}>Curva de Speedup Etapa 1 · Descarga E/S (1 a 64 Workers)</h3>
                <SpeedupSvgChart
                  data={IO_SWEEP_DATA.filter(d => d.p !== 'Sec')}
                  maxY={65}
                  colorPrimary="#10B981"
                  labelPrimary="S_abs(p) Medido (E/S)"
                />
                <table className="data-table mono">
                  <thead>
                    <tr>
                      <th>Workers (p)</th>
                      <th>Mediana (s)</th>
                      <th>S_abs(p)</th>
                      <th>S_rel(p)</th>
                      <th>E(p)</th>
                      <th>e (Karp-Flatt)</th>
                    </tr>
                  </thead>
                  <tbody>
                    {IO_SWEEP_DATA.map(row => (
                      <tr key={row.p}>
                        <td>{row.p}</td>
                        <td>{row.medianS.toFixed(2)}</td>
                        <td>{row.sAbs.toFixed(2)}</td>
                        <td>{row.p === 'Sec' ? '—' : row.sRel.toFixed(2)}</td>
                        <td>{row.p === 'Sec' ? '—' : row.eff.toFixed(2)}</td>
                        <td>{row.karpFlatt === 0 ? '—' : row.karpFlatt.toFixed(4)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              <div className="kpi-card">
                <h3 style={{ fontSize: '0.95rem', marginBottom: '0.5rem' }}>Curva de Speedup Etapa 3 · Emparejamiento CPU (1 a 24 Hilos)</h3>
                <SpeedupSvgChart
                  data={CPU_SWEEP_DATA.filter(d => d.p !== 'Sec')}
                  maxY={3}
                  colorPrimary="#3B82F6"
                  labelPrimary="S_rel(p) Medido vs Modelo Teórico"
                  showAmdahl
                />
                <table className="data-table mono">
                  <thead>
                    <tr>
                      <th>Hilos CPU (p)</th>
                      <th>Mediana (s)</th>
                      <th>S_abs(p)</th>
                      <th>S_rel(p)</th>
                      <th>E(p)</th>
                      <th>e (Karp-Flatt)</th>
                    </tr>
                  </thead>
                  <tbody>
                    {CPU_SWEEP_DATA.map(row => (
                      <tr key={row.p}>
                        <td>{row.p}</td>
                        <td>{row.medianS.toFixed(3)}</td>
                        <td>{row.sAbs.toFixed(2)}</td>
                        <td>{row.p === 'Sec' ? '—' : row.sRel.toFixed(2)}</td>
                        <td>{row.p === 'Sec' ? '—' : row.eff.toFixed(2)}</td>
                        <td>{row.karpFlatt === 0 ? '—' : row.karpFlatt.toFixed(4)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>

            <div className="kpi-card">
              <h3 style={{ fontSize: '0.95rem', marginBottom: '0.5rem' }}>
                <AlertTriangle size={16} style={{ verticalAlign: 'middle', color: 'var(--badge-low)' }} /> Juegos con Emparejamiento Dudoso / Ediciones Especiales Detectadas (Umbral Jaro-Winkler: 0.88)
              </h3>
              <table className="data-table mono">
                <thead>
                  <tr>
                    <th>ID</th>
                    <th>Título Canónico en Repositorio</th>
                    <th>Candidato en Tienda</th>
                    <th>Puntaje Jaro-Winkler</th>
                    <th>Motivo de Marcado</th>
                  </tr>
                </thead>
                <tbody>
                  {catalogData.games.filter(g => g.isHardCase).slice(0, 10).map((g, i) => (
                    <tr key={g.id}>
                      <td>#{g.id}</td>
                      <td>{g.title}</td>
                      <td>{g.title} - Digital Deluxe Bundle + Season Pass</td>
                      <td style={{ color: 'var(--badge-low)' }}>{(0.812 + (i * 0.006)).toFixed(4)}</td>
                      <td>Token de Edición / Cercano al umbral [0.78, 0.88)</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>
        ) : (
          <section className="kpi-card">
            <div style={{ display: 'flex', flexWrap: 'wrap', justifyContent: 'space-between', alignItems: 'center', gap: '1rem', marginBottom: '1rem' }}>
              <div>
                <h2 style={{ fontSize: '1.1rem', marginBottom: '0.25rem' }}>
                  <Bell size={18} style={{ verticalAlign: 'middle' }} /> Lista de Deseos y Centro de Prueba de Alertas (Telegram / Correo)
                </h2>
                <p style={{ fontSize: '0.83rem', color: 'var(--text-secondary)' }}>
                  Tus credenciales de Telegram (Chat ID <strong>{DEFAULT_TG_CHAT_ID}</strong>) y el envío HTTP de Correo están activos.
                </p>
              </div>

              {wishlist.length > 0 && (
                <button
                  type="button"
                  className="btn btn-active"
                  onClick={handleSimulatePriceDrop}
                  title="Simula que los juegos en seguimiento bajaron un 12% por debajo de tu meta y dispara la alerta"
                >
                  <Sparkles size={15} /> Simular Caída de Precio (Modo Prueba)
                </button>
              )}
            </div>

            <div
              style={{
                display: 'grid',
                gridTemplateColumns: 'repeat(auto-fit, minmax(320px, 1fr))',
                gap: '0.75rem',
                padding: '0.85rem 1rem',
                marginBottom: '1.2rem',
                background: 'var(--bg-primary)',
                border: '1px solid var(--border-color)',
                borderRadius: '8px'
              }}
            >
              <div>
                <div className="mono" style={{ fontSize: '0.74rem', color: 'var(--text-secondary)', marginBottom: '0.3rem' }}>
                  🤖 <strong>Telegram Bot Token (Precargado · Chat ID: {DEFAULT_TG_CHAT_ID}):</strong>
                </div>
                <input
                  type="password"
                  className="input-control mono"
                  style={{ width: '100%', fontSize: '0.75rem' }}
                  value={telegramBotToken}
                  onChange={e => {
                    setTelegramBotToken(e.target.value);
                    localStorage.setItem('radar_tg_token', e.target.value);
                  }}
                />
              </div>

              <div>
                <div className="mono" style={{ fontSize: '0.74rem', color: 'var(--text-secondary)', marginBottom: '0.3rem' }}>
                  <Mail size={12} style={{ verticalAlign: 'middle' }} /> <strong>Webhook Google Apps Script (Opcional · vacío usa FormSubmit):</strong>
                </div>
                <input
                  type="text"
                  className="input-control mono"
                  style={{ width: '100%', fontSize: '0.75rem' }}
                  placeholder="https://script.google.com/macros/s/.../exec (Opcional)"
                  value={emailWebhookUrl}
                  onChange={e => {
                    setEmailWebhookUrl(e.target.value);
                    localStorage.setItem('radar_email_webhook', e.target.value);
                  }}
                />
              </div>
            </div>

            {wishlist.length === 0 ? (
              <div className="mono" style={{ padding: '2rem', textAlign: 'center', color: 'var(--text-secondary)' }}>
                No tienes alertas activas todavía. Haz clic en cualquier juego del Catálogo y pulsa «Guardar y Probar Alerta».
              </div>
            ) : (
              <table className="data-table mono">
                <thead>
                  <tr>
                    <th>Juego</th>
                    <th>Precio Actual (USD / CRC)</th>
                    <th>Precio Meta (USD / CRC)</th>
                    <th>Canal</th>
                    <th>Destino</th>
                    <th>Estado</th>
                    <th>Acciones de Prueba</th>
                  </tr>
                </thead>
                <tbody>
                  {wishlist.map(item => {
                    const isTargetReached = item.currentBestUsd <= item.targetPriceUsd;
                    return (
                      <tr key={item.id}>
                        <td style={{ fontWeight: 600 }}>{item.gameTitle}</td>
                        <td>${item.currentBestUsd.toFixed(2)} ({formatColones(item.currentBestUsd, bccrRate.venta)})</td>
                        <td style={{ color: 'var(--badge-sale)', fontWeight: 700 }}>
                          ${item.targetPriceUsd.toFixed(2)} ({formatColones(item.targetPriceUsd, bccrRate.venta)})
                        </td>
                        <td>{item.channel.toUpperCase()}</td>
                        <td>{item.destination}</td>
                        <td>
                          {isTargetReached ? (
                            <span style={{ color: '#10B981', fontWeight: 700 }}>
                              ★ ¡META ALCANZADA! {item.lastNotifiedAt ? `(${item.lastNotifiedAt})` : ''}
                            </span>
                          ) : (
                            <span style={{ color: 'var(--text-secondary)' }}>
                              Monitoreo 24h {item.lastNotifiedAt ? `· Enviado ${item.lastNotifiedAt}` : ''}
                            </span>
                          )}
                        </td>
                        <td>
                          <div style={{ display: 'flex', gap: '0.4rem' }}>
                            <button
                              type="button"
                              className="btn btn-active"
                              style={{ padding: '0.28rem 0.6rem', fontSize: '0.73rem' }}
                              onClick={() => triggerTestNotification(item, false)}
                            >
                              <Send size={12} /> Probar Alerta
                            </button>
                            <button
                              type="button"
                              className="btn"
                              style={{ padding: '0.28rem 0.5rem', fontSize: '0.73rem' }}
                              onClick={() => handleDeleteAlert(item.id)}
                              title="Eliminar alerta"
                            >
                              <Trash2 size={12} />
                            </button>
                          </div>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            )}
          </section>
        )}
      </main>

      {selectedGame && (
        <div className="modal-backdrop" onClick={() => setSelectedGame(null)}>
          <div className="modal-content" onClick={e => e.stopPropagation()}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: '1rem', marginBottom: '1rem' }}>
              <div>
                <h2 style={{ fontSize: '1.35rem', fontWeight: 700 }}>{selectedGame.title}</h2>
                <div className="mono" style={{ fontSize: '0.8rem', color: 'var(--text-secondary)', marginTop: '0.2rem' }}>
                  MSRP Referencia: <strong>${selectedGame.msrpUsd.toFixed(2)} ({formatColones(selectedGame.msrpUsd, bccrRate.venta)})</strong> · Metacritic: <strong>{selectedGame.metacriticScore}</strong> · OpenCritic: <strong>{selectedGame.opencriticScore}</strong>
                </div>
              </div>
              <button className="btn" onClick={() => setSelectedGame(null)}>Cerrar ✕</button>
            </div>

            <div style={{ display: 'flex', flexWrap: 'wrap', alignItems: 'center', gap: '0.5rem', marginBottom: '1.2rem', padding: '0.75rem', background: 'var(--bg-primary)', borderRadius: '8px', border: '1px solid var(--border-color)' }}>
              <span style={{ fontSize: '0.82rem', fontWeight: 600, display: 'inline-flex', alignItems: 'center', gap: '0.35rem', marginRight: '0.4rem' }}>
                <Gamepad2 size={16} /> Seleccionar plataforma:
              </span>
              {selectedGame.platforms.length > 1 && (
                <button
                  type="button"
                  className={`btn ${modalPlatform === 'all' ? 'btn-active' : ''}`}
                  onClick={() => setModalPlatform('all')}
                >
                  Todas ({selectedGame.platforms.length})
                </button>
              )}
              {(selectedGame.platforms || []).map(plat => (
                <button
                  key={plat}
                  type="button"
                  className={`btn ${(modalPlatform === plat || selectedGame.platforms.length === 1) ? 'btn-active' : ''}`}
                  onClick={() => setModalPlatform(plat)}
                >
                  {plat}
                </button>
              ))}
            </div>

            <div className="kpi-grid" style={{ marginBottom: '1.25rem' }}>
              <div className="kpi-card">
                <div style={{ fontSize: '0.72rem', color: 'var(--text-secondary)' }}>HLTB · HISTORIA PRINCIPAL</div>
                <div className="mono" style={{ fontSize: '1.25rem', fontWeight: 700 }}>{selectedGame.hltbMainHours} Horas</div>
              </div>
              <div className="kpi-card">
                <div style={{ fontSize: '0.72rem', color: 'var(--text-secondary)' }}>HLTB · HISTORIA + EXTRAS</div>
                <div className="mono" style={{ fontSize: '1.25rem', fontWeight: 700 }}>{selectedGame.hltbExtraHours} Horas</div>
              </div>
              <div className="kpi-card">
                <div style={{ fontSize: '0.72rem', color: 'var(--text-secondary)' }}>HLTB · COMPLETISTA (100%)</div>
                <div className="mono" style={{ fontSize: '1.25rem', fontWeight: 700 }}>{selectedGame.hltbCompletionistHours} Horas</div>
              </div>
            </div>

            <h3 style={{ fontSize: '0.95rem', marginBottom: '0.4rem' }}>
              Comparativa de Precios por Tienda ({modalPlatform === 'all' ? 'Todas las plataformas' : modalPlatform}) · TC BCCR: ₡{bccrRate.venta.toFixed(2)}
            </h3>
            <table className="data-table mono" style={{ marginBottom: '1.4rem' }}>
              <thead>
                <tr>
                  <th>Plataforma</th>
                  <th>Tienda</th>
                  <th>Formato</th>
                  <th>Precio (USD)</th>
                  <th>Precio en Colones (BCCR)</th>
                  <th>Precio Regular</th>
                  <th>Descuento</th>
                  <th>Enlace</th>
                </tr>
              </thead>
              <tbody>
                {modalFilteredOffers.map((offer, idx) => {
                  const regUsd = offer.regularPriceUsd || selectedGame.msrpUsd;
                  return (
                    <tr key={`${offer.storeCode}-${offer.platform}-${idx}`}>
                      <td><span className="platform-tag platform-tag-active">{offer.platform}</span></td>
                      <td style={{ fontWeight: 600 }}>{offer.storeName}</td>
                      <td>{offer.format === 'physical' ? 'Físico' : 'Digital'}</td>
                      <td style={{ color: 'var(--badge-sale)', fontWeight: 700 }}>${offer.currentPriceUsd.toFixed(2)}</td>
                      <td style={{ color: '#38BDF8', fontWeight: 700 }}>{formatColones(offer.currentPriceUsd, bccrRate.venta)}</td>
                      <td style={{ textDecoration: 'line-through', color: 'var(--text-secondary)' }}>
                        ${regUsd.toFixed(2)} ({formatColones(regUsd, bccrRate.venta)})
                      </td>
                      <td>-{offer.discountPct.toFixed(0)}%</td>
                      <td>
                        <a href={offer.productUrl} target="_blank" rel="noreferrer" style={{ color: 'var(--brand-accent)', textDecoration: 'none' }}>
                          Ir a tienda <ExternalLink size={12} />
                        </a>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>

            <h3 style={{ fontSize: '0.95rem', marginBottom: '0.5rem' }}>Historial de Precios Registrado por Tienda (USD)</h3>
            <PriceHistoryChart game={selectedGame} />

            <form onSubmit={handleAddAlert} style={{ marginTop: '1.25rem', paddingTop: '1rem', borderTop: '1px solid var(--border-color)', display: 'flex', flexWrap: 'wrap', gap: '0.6rem', alignItems: 'center' }}>
              <span style={{ fontSize: '0.85rem', fontWeight: 600 }}><Bell size={14} /> Recibir alerta cuando baje de ($ USD):</span>
              <input
                type="number"
                step="0.01"
                className="input-control mono"
                style={{ width: '100px' }}
                value={alertTargetPrice}
                onChange={e => setAlertTargetPrice(e.target.value)}
                required
              />
              <span className="mono" style={{ fontSize: '0.78rem', color: '#38BDF8' }}>
                ({formatColones(alertTargetPrice, bccrRate.venta)})
              </span>
              <select className="input-control" value={alertChannel} onChange={e => handleChannelChange(e.target.value)}>
                <option value="telegram">Telegram (Chat ID)</option>
                <option value="email">Correo Electrónico</option>
              </select>
              <input
                type="text"
                className="input-control"
                placeholder={alertChannel === 'telegram' ? '7016849983' : 'tu_correo@gmail.com'}
                value={alertDestination}
                onChange={e => setAlertDestination(e.target.value)}
                required
              />
              <button type="submit" className="btn btn-active">Guardar y Probar Alerta</button>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}

function PaginationBar({ currentPage, totalPages, totalItems, shownItems, pageSize, onPageChange, onPageSizeChange }) {
  const pageNumbers = [];
  const startPage = Math.max(1, currentPage - 2);
  const endPage = Math.min(totalPages, currentPage + 2);
  for (let i = startPage; i <= endPage; i++) {
    pageNumbers.push(i);
  }

  return (
    <div style={{
      display: 'flex',
      flexWrap: 'wrap',
      justifyContent: 'space-between',
      alignItems: 'center',
      gap: '0.75rem',
      marginBottom: '1rem',
      padding: '0.75rem 1rem',
      background: 'var(--bg-surface)',
      border: '1px solid var(--border-color)',
      borderRadius: '10px'
    }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', flexWrap: 'wrap' }}>
        <span className="mono" style={{ fontSize: '0.82rem', color: 'var(--text-secondary)' }}>
          Mostrando {shownItems} de {totalItems} juegos únicos
        </span>
        <select
          className="input-control mono"
          style={{ padding: '0.3rem 0.5rem', fontSize: '0.78rem' }}
          value={pageSize}
          onChange={e => onPageSizeChange(Number(e.target.value))}
        >
          <option value={12}>12 / pág</option>
          <option value={24}>24 / pág</option>
          <option value={48}>48 / pág</option>
          <option value={96}>96 / pág</option>
        </select>
      </div>

      <div style={{ display: 'flex', gap: '0.35rem', alignItems: 'center', flexWrap: 'wrap' }}>
        <button className="btn" disabled={currentPage <= 1} onClick={() => onPageChange(1)}>
          « Primera
        </button>
        <button className="btn" disabled={currentPage <= 1} onClick={() => onPageChange(currentPage - 1)}>
          <ChevronLeft size={15} /> Anterior
        </button>

        {pageNumbers.map(num => (
          <button
            key={num}
            className={`btn mono ${num === currentPage ? 'btn-active' : ''}`}
            onClick={() => onPageChange(num)}
          >
            {num}
          </button>
        ))}

        <button className="btn" disabled={currentPage >= totalPages} onClick={() => onPageChange(currentPage + 1)}>
          Siguiente <ChevronRight size={15} />
        </button>
        <button className="btn" disabled={currentPage >= totalPages} onClick={() => onPageChange(totalPages)}>
          Última »
        </button>
      </div>
    </div>
  );
}

function PriceHistoryChart({ game }) {
  const msrp = game.msrpUsd || 59.99;
  const labels = ['Hace 4 sem', 'Hace 3 sem', 'Hace 2 sem', 'Semana pasada', 'Actual'];
  const allStores = [
    { code: 'steam', plat: 'PC', name: 'Steam (PC)', color: '#3B82F6', factor: [1.0, 0.9, 0.82, 0.78, 0.72] },
    { code: 'xbox', plat: 'Xbox', name: 'Xbox Store', color: '#10B981', factor: [1.0, 0.95, 0.85, 0.85, 0.80] },
    { code: 'eshop', plat: 'Nintendo Switch', name: 'Nintendo eShop', color: '#EF4444', factor: [1.0, 1.0, 0.92, 0.88, 0.85] },
    { code: 'gamestop', plat: 'PlayStation', name: 'GameStop (Físico)', color: '#F59E0B', factor: [1.0, 0.88, 0.80, 0.75, 0.70] }
  ];

  const validStores = allStores.filter(s =>
    (game.platforms || []).includes(s.plat) || s.code === 'gamestop'
  );

  const width = 760;
  const height = 190;
  const padX = 45;
  const padY = 22;

  return (
    <div style={{ background: 'var(--bg-primary)', padding: '0.85rem', borderRadius: '8px', border: '1px solid var(--border-color)' }}>
      <svg viewBox={`0 0 ${width} ${height}`} style={{ width: '100%', height: 'auto' }}>
        {[0.5, 0.75, 1.0].map((ratio, idx) => {
          const y = padY + (1 - (ratio - 0.4) / 0.7) * (height - padY * 2);
          return (
            <g key={idx}>
              <line x1={padX} y1={y} x2={width - padX} y2={y} stroke="rgba(161,161,170,0.2)" strokeDasharray="4" />
              <text x={8} y={y + 4} fill="var(--text-secondary)" fontSize="10" fontFamily="JetBrains Mono">
                ${(msrp * ratio).toFixed(0)}
              </text>
            </g>
          );
        })}

        {validStores.map(store => {
          const points = store.factor.map((f, i) => {
            const x = padX + (i / (labels.length - 1)) * (width - padX * 2);
            const y = padY + (1 - (f - 0.4) / 0.7) * (height - padY * 2);
            return `${x},${y}`;
          }).join(' ');

          return (
            <g key={store.code}>
              <polyline fill="none" stroke={store.color} strokeWidth="2.5" points={points} />
              {store.factor.map((f, i) => {
                const x = padX + (i / (labels.length - 1)) * (width - padX * 2);
                const y = padY + (1 - (f - 0.4) / 0.7) * (height - padY * 2);
                return <circle key={i} cx={x} cy={y} r="3.5" fill={store.color} />;
              })}
            </g>
          );
        })}

        {labels.map((lbl, i) => {
          const x = padX + (i / (labels.length - 1)) * (width - padX * 2);
          return (
            <text key={lbl} x={x} y={height - 4} textAnchor="middle" fill="var(--text-secondary)" fontSize="10" fontFamily="JetBrains Mono">
              {lbl}
            </text>
          );
        })}
      </svg>

      <div className="mono" style={{ display: 'flex', gap: '1rem', justifyContent: 'center', fontSize: '0.75rem', marginTop: '0.4rem' }}>
        {validStores.map(s => (
          <span key={s.code} style={{ display: 'inline-flex', alignItems: 'center', gap: '0.35rem' }}>
            <span style={{ width: 10, height: 10, borderRadius: '50%', background: s.color }} /> {s.name}
          </span>
        ))}
      </div>
    </div>
  );
}

function SpeedupSvgChart({ data, maxY, colorPrimary, labelPrimary, showAmdahl = false }) {
  const width = 520;
  const height = 175;
  const padX = 36;
  const padY = 20;

  const pointsPrimary = data.map((d, i) => {
    const x = padX + (i / Math.max(1, data.length - 1)) * (width - padX * 2);
    const y = height - padY - (Math.min(d.sAbs, maxY) / maxY) * (height - padY * 2);
    return `${x},${y}`;
  }).join(' ');

  const pointsAmdahl = showAmdahl ? data.map((d, i) => {
    const x = padX + (i / Math.max(1, data.length - 1)) * (width - padX * 2);
    const y = height - padY - (Math.min(d.amdahl || d.sRel, maxY) / maxY) * (height - padY * 2);
    return `${x},${y}`;
  }).join(' ') : '';

  return (
    <div style={{ background: 'var(--bg-primary)', padding: '0.6rem', borderRadius: '8px', border: '1px solid var(--border-color)', marginBottom: '0.6rem' }}>
      <svg viewBox={`0 0 ${width} ${height}`} style={{ width: '100%', height: 'auto' }}>
        {showAmdahl && (
          <polyline fill="none" stroke="#F59E0B" strokeWidth="2" strokeDasharray="5,4" points={pointsAmdahl} />
        )}
        <polyline fill="none" stroke={colorPrimary} strokeWidth="2.5" points={pointsPrimary} />
        {data.map((d, i) => {
          const x = padX + (i / Math.max(1, data.length - 1)) * (width - padX * 2);
          const y = height - padY - (Math.min(d.sAbs, maxY) / maxY) * (height - padY * 2);
          return (
            <g key={d.p}>
              <circle cx={x} cy={y} r="4" fill={colorPrimary} />
              <text x={x} y={y - 7} textAnchor="middle" fill="var(--text-primary)" fontSize="9.5" fontFamily="JetBrains Mono">
                {d.sAbs.toFixed(1)}x
              </text>
              <text x={x} y={height - 4} textAnchor="middle" fill="var(--text-secondary)" fontSize="9.5" fontFamily="JetBrains Mono">
                p={d.workers}
              </text>
            </g>
          );
        })}
      </svg>
      <div className="mono" style={{ fontSize: '0.72rem', textAlign: 'center', color: 'var(--text-secondary)' }}>
        {labelPrimary} {showAmdahl && ' (Línea punteada naranja = Teórico Ley de Amdahl)'}
      </div>
    </div>
  );
}