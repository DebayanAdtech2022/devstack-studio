// Smart OS & Architecture Detection for DevStack Studio
(function () {
  const titleEl = document.getElementById('rec-title');
  const metaEl = document.getElementById('rec-meta');
  const btnEl = document.getElementById('rec-btn');
  const iconEl = document.getElementById('rec-icon');
  const checksumEl = document.getElementById('rec-checksum');

  const userAgent = navigator.userAgent || '';
  const platform = navigator.platform || '';

  let os = 'Unknown';
  let arch = 'x64';
  let filename = 'devstack-studio-0.1.0-universal-offline.zip';
  let icon = '📦';
  let platformLabel = 'Universal Offline Bundle';
  let metaDesc = 'Multi-platform package containing all binaries';

  // OS Detection
  if (/Win/i.test(platform) || /Windows/i.test(userAgent)) {
    os = 'Windows';
    icon = '🪟';
    if (/ARM/i.test(userAgent) || /ARM64/i.test(navigator.userAgentData?.architecture || '')) {
      arch = 'arm64';
      filename = 'devstack-studio-0.1.0-win-arm64.zip';
      platformLabel = 'Windows 11 (ARM64)';
      metaDesc = 'Native 64-bit ARM binary (Snapdragon / Surface Pro)';
    } else {
      arch = 'x64';
      filename = 'devstack-studio-0.1.0-win-x64.zip';
      platformLabel = 'Windows 10 / 11 (x64)';
      metaDesc = 'Native self-contained single executable';
    }
  } else if (/Mac/i.test(platform) || /Macintosh/i.test(userAgent)) {
    os = 'macOS';
    icon = '🍎';
    // Mac detection - modern Macs are overwhelmingly Apple Silicon (arm64)
    // Note: navigator.platform on Apple Silicon still reports 'MacIntel' in many browsers
    if (/arm/i.test(navigator.userAgentData?.architecture || '') || (navigator.maxTouchPoints > 0 && !/iPhone|iPad/i.test(userAgent))) {
      arch = 'arm64';
      filename = 'devstack-studio-0.1.0-osx-arm64.zip';
      platformLabel = 'macOS (Apple Silicon M1-M4)';
      metaDesc = 'Native Apple Silicon ARM64 build';
    } else {
      // Default to Apple Silicon recommended with notice for Intel
      arch = 'arm64';
      filename = 'devstack-studio-0.1.0-osx-arm64.zip';
      platformLabel = 'macOS (Apple Silicon recommended)';
      metaDesc = 'Choose Intel build below if running on an Intel Mac';
    }
  } else if (/Linux/i.test(platform) || /Linux/i.test(userAgent)) {
    os = 'Linux';
    icon = '🐧';
    if (/aarch64|arm64/i.test(userAgent)) {
      arch = 'arm64';
      filename = 'devstack-studio-0.1.0-linux-arm64.zip';
      platformLabel = 'Linux (ARM64)';
      metaDesc = 'glibc 2.31+ binary for 64-bit ARM Linux';
    } else {
      arch = 'x64';
      filename = 'devstack-studio-0.1.0-linux-x64.zip';
      platformLabel = 'Linux (x86_64)';
      metaDesc = 'glibc 2.31+ binary for Ubuntu / Debian / Fedora / Arch';
    }
  }

  // Update DOM with detected platform recommendation
  if (titleEl && metaEl && btnEl && iconEl) {
    iconEl.textContent = icon;
    titleEl.textContent = platformLabel;
    metaEl.textContent = metaDesc;
    btnEl.textContent = `Download for ${os} (${arch})`;
    const releaseBase = "https://github.com/DebayanAdtech2022/devstack-studio/releases/download/v0.1.0/";
    btnEl.href = `${releaseBase}${filename}`;
  }

  if (checksumEl) {
    checksumEl.textContent = `Target: ${filename} | Verifiable via release-manifest.json`;
  }
})();
