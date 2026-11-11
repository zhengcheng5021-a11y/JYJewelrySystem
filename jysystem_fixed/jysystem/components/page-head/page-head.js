Component({
  properties: {
    eyebrow: { type: String, value: '' },
    title: { type: String, value: '' },
    subtitle: { type: String, value: '' },
    action: { type: String, value: '' }
  },
  methods: {
    onAction() { this.triggerEvent('action') }
  }
})
