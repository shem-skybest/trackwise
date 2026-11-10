import { inject, Injectable } from "@angular/core";
import { Api, getTenantRoles, getUsers, type RoleDto, type UserDto } from "@trackwise/shared/api";
import { from, map, Observable } from "rxjs";

@Injectable({ providedIn: "root" })
export class UserService {
  private readonly api = inject(Api);

  searchUser(searchQuery: string): Observable<UserDto[] | undefined> {
    const users$ = from(this.api.invoke(getUsers, { Search: searchQuery }));
    return users$.pipe(map((i) => i.items ?? undefined));
  }

  /**
   * The roles the signed-in user may put someone in. The API decides (`canAssign`: no handing out
   * a role with permissions you don't hold) and re-checks on save; this only trims the list.
   */
  fetchRoles(): Observable<RoleDto[]> {
    return from(this.api.invoke(getTenantRoles, { Page: 1, PageSize: 100 })).pipe(
      map((result) =>
        (result.items ?? [])
          .filter((role) => role.canAssign)
          .sort((a, b) => (a.displayName ?? "").localeCompare(b.displayName ?? "")),
      ),
    );
  }
}
