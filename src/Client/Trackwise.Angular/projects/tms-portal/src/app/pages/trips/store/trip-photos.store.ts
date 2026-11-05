import { inject } from "@angular/core";
import { patchState, signalStore, withHooks, withMethods, withState } from "@ngrx/signals";
import { getApiErrorMessage } from "@trackwise/shared";
import {
  addTripStopPhotos,
  Api,
  deleteTripStopPhoto,
  downloadDocument,
  getTripStopPhotos,
  type TripStopPhotoDto,
} from "@trackwise/shared/api";
import { ToastService } from "@trackwise/shared/services";

interface TripPhotosState {
  tripId: string | null;
  photos: TripStopPhotoDto[];
  /** Object URLs of the downloaded photos, by photo id - the download needs the auth header. */
  urls: Record<string, string>;
  /** Report blocks with an upload in flight, by block key. */
  uploading: string[];
}

const initialState: TripPhotosState = { tripId: null, photos: [], urls: {}, uploading: [] };

/** The crew report's photos, shared by every block on the page. Provide it on the page. */
export const TripPhotosStore = signalStore(
  withState(initialState),
  withMethods((store, api = inject(Api), toast = inject(ToastService)) => {
    const fetchUrls = async (photos: TripStopPhotoDto[]) => {
      const missing = photos.filter((p) => !store.urls()[p.id!]);
      const loaded = await Promise.all(
        missing.map(async (p) => {
          try {
            const blob = await api.invoke(downloadDocument, { documentId: p.id! });
            return [p.id!, URL.createObjectURL(blob)] as const;
          } catch {
            return null;
          }
        }),
      );
      const urls = Object.fromEntries(loaded.filter((e) => e !== null));
      patchState(store, (s) => ({ urls: { ...s.urls, ...urls } }));
    };

    return {
      async load(tripId: string): Promise<void> {
        patchState(store, { tripId });
        try {
          const photos = await api.invoke(getTripStopPhotos, { tripId });
          patchState(store, { photos });
          await fetchUrls(photos);
        } catch (error) {
          toast.showError(getApiErrorMessage(error, "Couldn't load the trip's photos."));
        }
      },

      async add(blockKey: string, stopIds: string[], files: File[]): Promise<void> {
        const tripId = store.tripId();
        if (!tripId || files.length === 0) return;

        patchState(store, (s) => ({ uploading: [...s.uploading, blockKey] }));
        try {
          const added = await api.invoke(addTripStopPhotos, {
            tripId,
            body: { StopIds: stopIds, Photos: files },
          });
          patchState(store, (s) => ({ photos: [...s.photos, ...added] }));
          await fetchUrls(added);
          toast.showSuccess(added.length === 1 ? "Photo added" : `${added.length} photos added`);
        } catch (error) {
          toast.showError(getApiErrorMessage(error, "Couldn't add the photos."));
        } finally {
          patchState(store, (s) => ({ uploading: s.uploading.filter((k) => k !== blockKey) }));
        }
      },

      async remove(photo: TripStopPhotoDto): Promise<void> {
        const tripId = store.tripId();
        if (!tripId) return;

        try {
          await api.invoke(deleteTripStopPhoto, { tripId, photoId: photo.id! });
          const url = store.urls()[photo.id!];
          if (url) URL.revokeObjectURL(url);
          patchState(store, (s) => ({ photos: s.photos.filter((p) => p.id !== photo.id) }));
          toast.showSuccess("Photo removed");
        } catch (error) {
          toast.showError(getApiErrorMessage(error, "Couldn't remove the photo."));
        }
      },
    };
  }),
  withHooks({
    onDestroy(store) {
      Object.values(store.urls()).forEach((url) => URL.revokeObjectURL(url));
    },
  }),
);
