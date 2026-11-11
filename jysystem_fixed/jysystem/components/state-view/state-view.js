Component({
  properties: {
    kind: { type: String, value: 'empty' },
    title: { type: String, value: '' },
    detail: { type: String, value: '' },
    action: { type: String, value: '' }
  },
  methods: {
    tapAction() { this.triggerEvent('action') }
  }
})
