# HotelBookingUi

This project was generated using [Angular CLI](https://github.com/angular/angular-cli) version 21.1.1.

## Development server

To start a local development server, run:

```bash
ng serve
```

Once the server is running, open your browser and navigate to `http://localhost:4200/`. The application will automatically reload whenever you modify any of the source files.

## Code scaffolding

Angular CLI includes powerful code scaffolding tools. To generate a new component, run:

```bash
ng generate component component-name
```

For a complete list of available schematics (such as `components`, `directives`, or `pipes`), run:

```bash
ng generate --help
```

## Building

To build the project run:

```bash
ng build
```

This will compile your project and store the build artifacts in the `dist/` directory. By default, the production build optimizes your application for performance and speed.

## Running unit tests

To execute unit tests with the [Vitest](https://vitest.dev/) test runner, use the following command:

```bash
ng test
```

## Running end-to-end tests

For end-to-end (e2e) testing, run:

```bash
ng e2e
```

Angular CLI does not come with an end-to-end testing framework by default. You can choose one that suits your needs.

## Additional Resources

For more information on using the Angular CLI, including detailed command references, visit the [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli) page.

## Deploy on Vercel

This frontend is ready for Vercel static deployment.

### 1) Set Vercel project root

If your repository contains both frontend and backend folders, set the **Root Directory** in Vercel to:

`hotel-booking-ui`

### 2) Configure environment variable

In Vercel project settings, add:

- `API_BASE_URL` = your deployed backend base URL (example: `https://your-backend-domain.com/api`)

This value is injected into `src/environments/environment.production.ts` during build.

### 3) Deploy

Vercel uses `vercel.json` in this project with:

- install command: `npm install`
- build command: `npm run build:vercel`
- output directory: `dist/hotel-booking-ui`

### 4) Local check before deploy

Run:

```bash
npm run build:vercel
```

If build succeeds locally, Vercel build should also succeed with the same configuration.
