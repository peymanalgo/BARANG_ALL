(() => {
  document.addEventListener('pointerdown', event => {
    const node = event.target.closest?.('.workflow-node');
    if (node && event.pointerId !== undefined && node.setPointerCapture) node.setPointerCapture(event.pointerId);
  }, { passive: true });
})();
