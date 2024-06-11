SDK update release:

Version number format X.Z.ZZ

1. Set incremented value at `KibotuUnityVersion`
2. Commit and push (any branch)
3. Create tag named `X.Z.ZZ`. Push tags.
4. Consume from client via
```
   "ai.kibotu.unity": "https://github.com/KibotuAI/kibotu-unity.git#X.Z.ZZ",
```