/* BPMS pages helpers: print + JSON export of instance timeline. No dependencies. */
(() => {
  'use strict';
  window.barangWorkflowBpms = {
    print() { window.print(); },
    downloadJson(name, obj) {
      const text = JSON.stringify(obj, null, 2);
      const blob = new Blob([text], { type: 'application/json;charset=utf-8' });
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url; a.download = name;
      document.body.appendChild(a); a.click(); a.remove();
      setTimeout(() => URL.revokeObjectURL(url), 4000);
    },
  };
})();
